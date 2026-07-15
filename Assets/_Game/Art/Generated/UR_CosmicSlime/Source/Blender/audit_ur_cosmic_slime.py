import json
import math
import os
from pathlib import Path

import bpy
from mathutils import Vector


ROOT = Path(os.environ["BUBBLEMIND_PROJECT_ROOT"])
FBX_PATH = ROOT / "Assets/_Game/Art/Generated/UR_CosmicSlime/Runtime/UR_CosmicSlime.fbx"
REPORT_PATH = ROOT / "Artifacts/UR_CosmicSlime/Blender/geometry_audit.json"
REQUIRED_OBJECTS = {
    "CharacterRoot",
    "SlimeBody",
    "NebulaInner",
    "Horn_L",
    "Horn_Center",
    "Horn_RightFluid",
    "Eye_L",
    "Eye_R",
    "StarCloudPoints",
    "SingularityAccretionRig",
    "SingularityCore",
    "SingularityAccretion",
    "AccretionSpiral_01",
    "AccretionSpiral_02",
    "AccretionSpiral_03",
    "OrbitRig_Lower",
    "OrbitRig_Upper",
    "OrbitBand_Lower",
    "OrbitBand_Upper",
    "RightHandSocket",
    "LeftHandSocket",
    "SkillVfxSocket",
    "ProjectileSocket",
    "GroundVfxSocket",
    "TargetSocket",
    "HealthBarSocket",
}
REQUIRED_SHAPE_KEYS = {"IdleBreath", "Squash", "Stretch", "UltimateCollapse"}
REQUIRED_MATERIALS = {
    "MAT_BlackCore",
    "MAT_Energy",
    "MAT_Nebula",
    "MAT_Orbit",
    "MAT_OrbitTrim",
    "MAT_Shell",
}


def shape_key_metrics(obj):
    if not obj or not obj.data.shape_keys:
        return {"names": [], "maximum_delta_m": {}}
    basis = obj.data.shape_keys.key_blocks.get("Basis")
    names = [key.name for key in obj.data.shape_keys.key_blocks if key.name != "Basis"]
    maximum_delta = {}
    if basis:
        for key in obj.data.shape_keys.key_blocks:
            if key.name == "Basis":
                continue
            maximum_delta[key.name] = max(
                (key.data[index].co - basis.data[index].co).length
                for index in range(len(basis.data))
            )
    return {"names": names, "maximum_delta_m": maximum_delta}


def collect_scene_metrics():
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
        object_bounds = [obj.matrix_world @ vertex.co for vertex in mesh.vertices]
        object_minimum = [min(point[index] for point in object_bounds) for index in range(3)]
        object_maximum = [max(point[index] for point in object_bounds) for index in range(3)]
        object_dimensions = [object_maximum[index] - object_minimum[index] for index in range(3)]
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

    minimum = [min(point[index] for point in bounds) for index in range(3)]
    maximum = [max(point[index] for point in bounds) for index in range(3)]
    dimensions = [maximum[index] - minimum[index] for index in range(3)]
    return {
        "triangles": triangles,
        "materials": sorted(materials),
        "dimensions": dimensions,
        "minimum": minimum,
        "maximum": maximum,
        "mesh_objects": sorted(mesh_objects, key=lambda item: item["name"]),
        "mesh_metrics": mesh_metrics,
    }


source_metrics = collect_scene_metrics()
source_names = set(bpy.data.objects.keys())
body_shape_keys = shape_key_metrics(bpy.data.objects.get("SlimeBody"))
inner_shape_keys = shape_key_metrics(bpy.data.objects.get("NebulaInner"))
shell_material = bpy.data.materials.get("MAT_Shell")
black_core_material = bpy.data.materials.get("MAT_BlackCore")
shell_rgba = list(shell_material.diffuse_color) if shell_material else []
black_core_rgba = list(black_core_material.diffuse_color) if black_core_material else []
core_metrics = source_metrics["mesh_metrics"].get("SingularityCore", {})
accretion_metrics = source_metrics["mesh_metrics"].get("SingularityAccretion", {})
body_metrics = source_metrics["mesh_metrics"].get("SlimeBody", {})
lower_band_metrics = source_metrics["mesh_metrics"].get("OrbitBand_Lower", {})
lower_trim_metrics = source_metrics["mesh_metrics"].get("OrbitTrim_Lower", {})
core_dimensions = core_metrics.get("dimensions_xyz", [0.0, 0.0, 0.0])
accretion_dimensions = accretion_metrics.get("dimensions_xyz", [0.0, 0.0, 0.0])
body_dimensions = body_metrics.get("dimensions_xyz", [0.0, 0.0, 0.0])
lower_orbit_max_z = max(
    lower_band_metrics.get("bounds_max_z", float("inf")),
    lower_trim_metrics.get("bounds_max_z", float("inf")),
)
core_min_z = core_metrics.get("bounds_min_z", float("-inf"))

source_body_names = set(body_shape_keys["names"])
source_inner_names = set(inner_shape_keys["names"])
shape_delta_thresholds = {
    "IdleBreath": 0.025,
    "Squash": 0.25,
    "Stretch": 0.25,
    "UltimateCollapse": 0.45,
}
shape_delta_passes = {
    name: body_shape_keys["maximum_delta_m"].get(name, 0.0) >= threshold
    for name, threshold in shape_delta_thresholds.items()
}

# FBX is re-imported into a temporary scene to prove that Unity-facing blend shapes exist in the export.
original_scene = bpy.context.scene
audit_scene = bpy.data.scenes.new("FBXExportAudit")
bpy.context.window.scene = audit_scene
bpy.ops.import_scene.fbx(filepath=str(FBX_PATH), use_anim=True)
exported_body = bpy.data.objects.get("SlimeBody")
if exported_body and exported_body.users_scene and audit_scene not in exported_body.users_scene:
    exported_body = next((obj for obj in audit_scene.objects if obj.name.startswith("SlimeBody")), None)
exported_shape_keys = shape_key_metrics(exported_body)
exported_shape_names = set(exported_shape_keys["names"])
bpy.context.window.scene = original_scene
bpy.data.scenes.remove(audit_scene)

report = {
    "triangles": source_metrics["triangles"],
    "triangle_budget": 20000,
    "materials": source_metrics["materials"],
    "material_count": len(source_metrics["materials"]),
    "dimensions_blender_xyz_m": source_metrics["dimensions"],
    "bounds_min_blender_xyz_m": source_metrics["minimum"],
    "bounds_max_blender_xyz_m": source_metrics["maximum"],
    "mesh_objects": source_metrics["mesh_objects"],
    "shape_keys": {
        "source_slime_body": body_shape_keys,
        "source_nebula_inner": inner_shape_keys,
        "exported_fbx_slime_body": exported_shape_keys,
        "required": sorted(REQUIRED_SHAPE_KEYS),
        "delta_thresholds_m": shape_delta_thresholds,
        "delta_passes": shape_delta_passes,
    },
    "visual_metrics": {
        "shell_rgba": shell_rgba,
        "black_core_rgba": black_core_rgba,
        "event_horizon_dimensions_xyz": core_dimensions,
        "accretion_dimensions_xyz": accretion_dimensions,
        "body_dimensions_xyz": body_dimensions,
        "body_height_to_width_ratio": body_dimensions[2] / max(body_dimensions[0], 0.0001),
        "event_horizon_min_z": core_min_z,
        "lower_orbit_max_z": lower_orbit_max_z,
    },
    "missing_required_objects": sorted(REQUIRED_OBJECTS - source_names),
    "missing_required_materials": sorted(REQUIRED_MATERIALS - set(source_metrics["materials"])),
    "passes": {
        "triangles": source_metrics["triangles"] <= 20000,
        "stable_material_slots": set(source_metrics["materials"]) == REQUIRED_MATERIALS,
        "required_objects": REQUIRED_OBJECTS <= source_names,
        "grounded": -0.005 <= source_metrics["minimum"][2] <= 0.025,
        "root_at_origin": tuple(round(value, 6) for value in bpy.data.objects["CharacterRoot"].location) == (0.0, 0.0, 0.0),
        "soft_squat_body": body_dimensions[2] / max(body_dimensions[0], 0.0001) <= 0.72,
        "near_black_shell": bool(shell_rgba) and max(shell_rgba[:3]) <= 0.06 and shell_rgba[3] >= 0.85,
        "black_event_horizon": bool(black_core_rgba) and max(black_core_rgba[:3]) <= 0.001,
        "readable_event_horizon": core_dimensions[0] >= 0.55 and core_dimensions[2] >= 0.55,
        "three_quarter_accretion_volume": accretion_dimensions[0] >= 0.75 and accretion_dimensions[2] >= 0.68,
        "lower_orbit_clears_core": lower_orbit_max_z <= core_min_z,
        "source_body_shape_keys": REQUIRED_SHAPE_KEYS <= source_body_names,
        "source_inner_shape_keys": REQUIRED_SHAPE_KEYS <= source_inner_names,
        "shape_key_deltas": all(shape_delta_passes.values()),
        "fbx_shape_keys": REQUIRED_SHAPE_KEYS <= exported_shape_names,
    },
}

REPORT_PATH.parent.mkdir(parents=True, exist_ok=True)
REPORT_PATH.write_text(json.dumps(report, indent=2), encoding="utf-8")
print("BUBBLEMIND_AUDIT=" + json.dumps(report, separators=(",", ":")))
if not all(report["passes"].values()):
    failed = [name for name, passed in report["passes"].items() if not passed]
    raise RuntimeError("UR Cosmic Slime audit failed: " + ", ".join(failed))
