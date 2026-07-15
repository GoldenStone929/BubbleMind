import json
import math
import os
from pathlib import Path

import bpy
from mathutils import Vector


PROJECT_ROOT = Path(os.environ["BUBBLEMIND_PROJECT_ROOT"]).resolve()
RUNTIME_DIR = PROJECT_ROOT / "Assets/_Game/Art/Generated/BasicElementSlimes/Runtime"
REPORT_PATH = PROJECT_ROOT / "Artifacts/BasicElementSlimes/Blender/geometry_audit.json"
ELEMENT_ORDER = ("Water", "Fire", "Earth", "Wind", "Lightning")
TRIANGLE_BUDGET = 2500

ELEMENT_PREFIXES = {
    "Water": ("WaterCrest", "WaterWave_", "Bubble_"),
    "Fire": ("Flame_",),
    "Earth": ("Rock_", "Leaf_"),
    "Wind": ("Wind_",),
    "Lightning": ("Spark_",),
}


def evaluated_triangle_count(obj, depsgraph):
    evaluated = obj.evaluated_get(depsgraph)
    mesh = evaluated.to_mesh()
    try:
        mesh.calc_loop_triangles()
        return len(mesh.loop_triangles)
    finally:
        evaluated.to_mesh_clear()


def object_world_bounds(obj):
    return [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]


def hierarchy(root):
    return [root] + list(root.children_recursive)


def normalize_material_name(name):
    return name.rsplit(".", 1)[0] if len(name) > 4 and name[-4] == "." and name[-3:].isdigit() else name


def collect_metrics(objects, use_export_names):
    depsgraph = bpy.context.evaluated_depsgraph_get()
    mesh_objects = [obj for obj in objects if obj.type == "MESH"]
    triangles = 0
    bounds = []
    materials = set()
    object_metrics = []
    names = []

    for obj in objects:
        name = obj.get("export_name", obj.name) if use_export_names else obj.name
        names.append(name)
        if obj.type != "MESH":
            continue
        count = evaluated_triangle_count(obj, depsgraph)
        triangles += count
        object_bounds = object_world_bounds(obj)
        bounds.extend(object_bounds)
        for slot in obj.material_slots:
            if slot.material:
                materials.add(normalize_material_name(slot.material.name))
        minimum = [min(point[index] for point in object_bounds) for index in range(3)]
        maximum = [max(point[index] for point in object_bounds) for index in range(3)]
        object_metrics.append(
            {
                "name": name,
                "triangles": count,
                "bounds_min_xyz": minimum,
                "bounds_max_xyz": maximum,
            }
        )

    if not bounds:
        return {
            "triangles": 0,
            "materials": [],
            "material_count": 0,
            "object_names": sorted(names),
            "mesh_objects": [],
            "bounds_min_xyz": [0.0, 0.0, 0.0],
            "bounds_max_xyz": [0.0, 0.0, 0.0],
            "dimensions_xyz": [0.0, 0.0, 0.0],
        }

    minimum = [min(point[index] for point in bounds) for index in range(3)]
    maximum = [max(point[index] for point in bounds) for index in range(3)]
    return {
        "triangles": triangles,
        "materials": sorted(materials),
        "material_count": len(materials),
        "object_names": sorted(names),
        "mesh_objects": sorted(object_metrics, key=lambda item: item["name"]),
        "bounds_min_xyz": minimum,
        "bounds_max_xyz": maximum,
        "dimensions_xyz": [maximum[index] - minimum[index] for index in range(3)],
    }


def required_names(element):
    return {
        "CharacterRoot",
        "ModelRoot",
        "SlimeBody",
        f"Element_{element}",
        "FaceDark_Pupil_L",
        "FaceDark_Pupil_R",
        "EyeHighlight_L",
        "EyeHighlight_R",
        "RightHandSocket",
        "LeftHandSocket",
        "SkillVfxSocket",
        "ProjectileSocket",
        "GroundVfxSocket",
        "TargetSocket",
        "HealthBarSocket",
    }


def expected_materials(element):
    return {
        f"MAT_BasicSlime_{element}_Body",
        f"MAT_BasicSlime_{element}_Accent",
        f"MAT_BasicSlime_{element}_Detail",
        "MAT_BasicSlime_EyeWhite",
        "MAT_BasicSlime_FaceDark",
        "MAT_BasicSlime_EyeHighlight",
    }


def has_element_detail(element, names):
    return any(name.startswith(prefix) for name in names for prefix in ELEMENT_PREFIXES[element])


def front_face_is_negative_y(objects, use_export_names):
    eye_centers = []
    for obj in objects:
        name = obj.get("export_name", obj.name) if use_export_names else obj.name
        if name in {"FaceDark_Pupil_L", "FaceDark_Pupil_R"}:
            eye_centers.append((obj.matrix_world.translation.y, obj.matrix_world.translation.z))
    return len(eye_centers) == 2 and all(y < -0.20 and z > 0.30 for y, z in eye_centers)


def build_passes(element, metrics, objects, use_export_names, fbx_exists=True):
    names = set(metrics["object_names"])
    missing = sorted(required_names(element) - names)
    missing_materials = sorted(expected_materials(element) - set(metrics["materials"]))
    dimensions = metrics["dimensions_xyz"]
    minimum = metrics["bounds_min_xyz"]
    grounded = -0.02 <= minimum[2] <= 0.08
    dimensions_valid = 0.85 <= dimensions[0] <= 1.90 and 0.60 <= dimensions[1] <= 1.60 and 0.90 <= dimensions[2] <= 1.85
    return {
        "fbx_exists": fbx_exists,
        "triangle_budget": 0 < metrics["triangles"] <= TRIANGLE_BUDGET,
        "required_hierarchy": not missing,
        "required_materials": not missing_materials,
        "material_budget": 1 <= metrics["material_count"] <= 6,
        "grounded": grounded,
        "dimensions": dimensions_valid,
        "front_faces_negative_y": front_face_is_negative_y(objects, use_export_names),
        "element_detail": has_element_detail(element, names),
        "eye_highlights": {"EyeHighlight_L", "EyeHighlight_R"}.issubset(names),
    }, missing, missing_materials


def find_source_root(element):
    candidates = [
        obj
        for obj in bpy.context.scene.objects
        if obj.get("bubble_mind_element") == element and obj.get("part_kind") == "root"
    ]
    if len(candidates) != 1:
        raise RuntimeError(f"Expected one source root for {element}; found {len(candidates)}")
    return candidates[0]


def audit_source(element):
    root = find_source_root(element)
    original_location = root.location.copy()
    root.location = (0.0, 0.0, 0.0)
    bpy.context.view_layer.update()
    objects = hierarchy(root)
    try:
        metrics = collect_metrics(objects, use_export_names=True)
        passes, missing, missing_materials = build_passes(element, metrics, objects, use_export_names=True)
        metrics["missing_required_objects"] = missing
        metrics["missing_required_materials"] = missing_materials
        metrics["passes"] = passes
        return metrics
    finally:
        root.location = original_location
        bpy.context.view_layer.update()


def clear_for_import():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for datablocks in (bpy.data.meshes, bpy.data.curves, bpy.data.materials):
        for datablock in list(datablocks):
            if datablock.users == 0:
                datablocks.remove(datablock)


def audit_fbx(element):
    path = RUNTIME_DIR / f"BasicSlime_{element}.fbx"
    if not path.exists() or path.stat().st_size <= 0:
        empty_metrics = collect_metrics([], use_export_names=False)
        passes, missing, missing_materials = build_passes(
            element,
            empty_metrics,
            [],
            use_export_names=False,
            fbx_exists=False,
        )
        empty_metrics.update(
            {
                "path": str(path),
                "file_size_bytes": 0,
                "missing_required_objects": missing,
                "missing_required_materials": missing_materials,
                "passes": passes,
            }
        )
        return empty_metrics

    clear_for_import()
    bpy.ops.import_scene.fbx(filepath=str(path), use_custom_props=False)
    # The source uses Blender Z-up and exports through the project's proven
    # Unity FBX convention (Y-up, -Z forward). Blender's FBX importer exposes
    # that baked basis as X, -Z, Y, so normalize it back before geometric QA.
    imported_root = bpy.data.objects.get("CharacterRoot")
    if imported_root is not None:
        imported_root.rotation_euler.x -= math.radians(90.0)
    bpy.context.view_layer.update()
    objects = list(bpy.context.scene.objects)
    metrics = collect_metrics(objects, use_export_names=False)
    passes, missing, missing_materials = build_passes(element, metrics, objects, use_export_names=False)
    metrics.update(
        {
            "path": str(path),
            "file_size_bytes": path.stat().st_size,
            "axis_normalization": "Unity Y-up/-Z-forward FBX normalized to Blender Z-up by -90 degrees X",
            "missing_required_objects": missing,
            "missing_required_materials": missing_materials,
            "passes": passes,
        }
    )
    return metrics


def all_pass(passes):
    return all(bool(value) for value in passes.values())


def main():
    source_results = {element: audit_source(element) for element in ELEMENT_ORDER}
    fbx_results = {element: audit_fbx(element) for element in ELEMENT_ORDER}
    elements = {}
    for element in ELEMENT_ORDER:
        source = source_results[element]
        fbx = fbx_results[element]
        elements[element] = {
            "source": source,
            "fbx": fbx,
            "passes": {
                "source": all_pass(source["passes"]),
                "fbx": all_pass(fbx["passes"]),
                "source_fbx_triangle_match": source["triangles"] == fbx["triangles"],
            },
        }

    report = {
        "asset_family": "BasicElementSlimes",
        "triangle_budget_per_model": TRIANGLE_BUDGET,
        "elements": elements,
    }
    report["all_passed"] = all(all_pass(entry["passes"]) for entry in elements.values())
    REPORT_PATH.parent.mkdir(parents=True, exist_ok=True)
    REPORT_PATH.write_text(json.dumps(report, indent=2), encoding="utf-8")
    print("BUBBLEMIND_BASIC_SLIME_AUDIT=" + json.dumps(report, separators=(",", ":")))
    if not report["all_passed"]:
        raise RuntimeError(f"Basic Element Slime audit failed; see {REPORT_PATH}")
    print("BUBBLEMIND_BASIC_SLIME_AUDIT_PASS")


main()
