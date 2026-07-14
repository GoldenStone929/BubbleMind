import json
import os
from pathlib import Path

import bpy
from mathutils import Vector


ROOT = Path(os.environ["BUBBLEMIND_PROJECT_ROOT"])
REPORT_PATH = ROOT / "Artifacts/UR_CosmicSlime/Blender/geometry_audit.json"
REQUIRED = {
    "CharacterRoot", "SlimeBody", "Horn_L", "Horn_R", "Eye_L", "Eye_R",
    "SingularityCore", "SingularityAccretion", "OrbitRing_A1", "OrbitRing_A2",
    "OrbitRing_B1", "OrbitRing_B2", "RightHandSocket", "LeftHandSocket",
    "SkillVfxSocket", "ProjectileSocket", "GroundVfxSocket", "TargetSocket",
    "HealthBarSocket",
}


depsgraph = bpy.context.evaluated_depsgraph_get()
triangles = 0
mesh_objects = []
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
    mesh_objects.append({"name": obj.name, "triangles": count})
    for slot in obj.material_slots:
        if slot.material and not slot.material.name.startswith("Preview"):
            materials.add(slot.material.name)
    for corner in obj.bound_box:
        bounds.append(obj.matrix_world @ Vector(corner))
    evaluated.to_mesh_clear()

minimum = [min(point[i] for point in bounds) for i in range(3)]
maximum = [max(point[i] for point in bounds) for i in range(3)]
dimensions = [maximum[i] - minimum[i] for i in range(3)]
missing = sorted(REQUIRED - set(bpy.data.objects.keys()))
report = {
    "triangles": triangles,
    "triangle_budget": 20000,
    "materials": sorted(materials),
    "material_count": len(materials),
    "dimensions_blender_xyz_m": dimensions,
    "bounds_min_blender_xyz_m": minimum,
    "bounds_max_blender_xyz_m": maximum,
    "mesh_objects": sorted(mesh_objects, key=lambda item: item["name"]),
    "missing_required_objects": missing,
    "passes": {
        "triangles": triangles <= 20000,
        "materials": len(materials) <= 3,
        "required_objects": not missing,
        "grounded": minimum[2] >= -0.02,
    },
}

REPORT_PATH.parent.mkdir(parents=True, exist_ok=True)
REPORT_PATH.write_text(json.dumps(report, indent=2), encoding="utf-8")
print("BUBBLEMIND_AUDIT=" + json.dumps(report, separators=(",", ":")))
