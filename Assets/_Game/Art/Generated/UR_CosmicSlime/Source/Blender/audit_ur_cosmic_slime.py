import json
import os
from pathlib import Path

import bpy
from mathutils import Vector


ROOT = Path(os.environ["BUBBLEMIND_PROJECT_ROOT"])
REPORT_PATH = ROOT / "Artifacts/UR_CosmicSlime/Blender/geometry_audit.json"
REQUIRED = {
    "CharacterRoot", "SlimeBody", "NebulaInner", "Horn_L", "Horn_Center",
    "Horn_RightFluid", "Eye_L", "Eye_R", "StarCloudPoints",
    "SingularityCore", "SingularityAccretion", "OrbitRig_Lower", "OrbitRig_Upper",
    "OrbitBand_Lower", "OrbitBand_Upper", "RightHandSocket", "LeftHandSocket",
    "SkillVfxSocket", "ProjectileSocket", "GroundVfxSocket", "TargetSocket",
    "HealthBarSocket",
}


depsgraph = bpy.context.evaluated_depsgraph_get()
triangles = 0
mesh_objects = []
mesh_metrics = {}
materials = set()
bounds = []

for obj in bpy.context.scene.objects:
    if obj.type != "MESH" or obj.name.startswith("Preview"):
        continue
    evaluated = obj.evaluated_get(depsgraph)
    mesh = evaluated.to_mesh()
    mesh.calc_loop_triangles()
    count = len(mesh.loop_triangles)
    triangles += count
    object_bounds = [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]
    object_minimum = [min(point[i] for point in object_bounds) for i in range(3)]
    object_maximum = [max(point[i] for point in object_bounds) for i in range(3)]
    object_dimensions = [object_maximum[i] - object_minimum[i] for i in range(3)]
    metrics = {
        "name": obj.name,
        "triangles": count,
        "bounds_min_z": object_minimum[2],
        "bounds_max_z": object_maximum[2],
        "dimensions_xyz": object_dimensions,
    }
    mesh_objects.append(metrics)
    mesh_metrics[obj.name] = metrics
    for slot in obj.material_slots:
        if slot.material and not slot.material.name.startswith("Preview"):
            materials.add(slot.material.name)
    bounds.extend(object_bounds)
    evaluated.to_mesh_clear()

minimum = [min(point[i] for point in bounds) for i in range(3)]
maximum = [max(point[i] for point in bounds) for i in range(3)]
dimensions = [maximum[i] - minimum[i] for i in range(3)]
missing = sorted(REQUIRED - set(bpy.data.objects.keys()))
shell_material = bpy.data.materials.get("MAT_Shell")
black_core_material = bpy.data.materials.get("MAT_BlackCore")
shell_rgba = list(shell_material.diffuse_color) if shell_material else []
black_core_rgba = list(black_core_material.diffuse_color) if black_core_material else []
core_metrics = mesh_metrics.get("SingularityCore", {})
accretion_metrics = mesh_metrics.get("SingularityAccretion", {})
lower_band_metrics = mesh_metrics.get("OrbitBand_Lower", {})
lower_trim_metrics = mesh_metrics.get("OrbitTrim_Lower", {})
core_dimensions = core_metrics.get("dimensions_xyz", [0.0, 0.0, 0.0])
accretion_dimensions = accretion_metrics.get("dimensions_xyz", [0.0, 0.0, 0.0])
lower_orbit_max_z = max(
    lower_band_metrics.get("bounds_max_z", float("inf")),
    lower_trim_metrics.get("bounds_max_z", float("inf")),
)
core_min_z = core_metrics.get("bounds_min_z", float("-inf"))
report = {
    "triangles": triangles,
    "triangle_budget": 20000,
    "materials": sorted(materials),
    "material_count": len(materials),
    "dimensions_blender_xyz_m": dimensions,
    "bounds_min_blender_xyz_m": minimum,
    "bounds_max_blender_xyz_m": maximum,
    "mesh_objects": sorted(mesh_objects, key=lambda item: item["name"]),
    "visual_metrics": {
        "shell_rgba": shell_rgba,
        "black_core_rgba": black_core_rgba,
        "event_horizon_dimensions_xyz": core_dimensions,
        "accretion_dimensions_xyz": accretion_dimensions,
        "event_horizon_min_z": core_min_z,
        "lower_orbit_max_z": lower_orbit_max_z,
    },
    "missing_required_objects": missing,
    "passes": {
        "triangles": triangles <= 20000,
        "materials": len(materials) <= 6,
        "required_objects": not missing,
        "grounded": minimum[2] >= -0.02,
        "near_black_shell": bool(shell_rgba) and max(shell_rgba[:3]) <= 0.015 and shell_rgba[3] >= 0.75,
        "black_event_horizon": bool(black_core_rgba) and max(black_core_rgba[:3]) <= 0.001,
        "volumetric_event_horizon": min(core_dimensions) >= 0.34,
        "side_readable_accretion": accretion_dimensions[0] >= 0.55 and accretion_dimensions[1] >= 0.25,
        "lower_orbit_clears_core": lower_orbit_max_z <= core_min_z,
    },
}

REPORT_PATH.parent.mkdir(parents=True, exist_ok=True)
REPORT_PATH.write_text(json.dumps(report, indent=2), encoding="utf-8")
print("BUBBLEMIND_AUDIT=" + json.dumps(report, separators=(",", ":")))
