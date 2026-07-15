import math
import os
from pathlib import Path

import bpy
from mathutils import Matrix, Vector


SOURCE_DIR = Path(__file__).resolve().parent
PROJECT_ROOT = Path(os.environ.get("BUBBLEMIND_PROJECT_ROOT", SOURCE_DIR.parents[6])).resolve()
ARTIFACT_DIR = PROJECT_ROOT / "Artifacts" / "UR_CosmicSlime" / "Blender"
BLEND_PATH = SOURCE_DIR / "UR_CosmicSlime.blend"
FBX_PATH = PROJECT_ROOT / "Assets/_Game/Art/Generated/UR_CosmicSlime/Runtime/UR_CosmicSlime.fbx"
PREVIEW_PATH = ARTIFACT_DIR / "UR_CosmicSlime_preview.png"
PREVIEW_SIDE_PATH = ARTIFACT_DIR / "UR_CosmicSlime_preview_side.png"
PREVIEW_BACK_PATH = ARTIFACT_DIR / "UR_CosmicSlime_preview_back.png"
PREVIEW_THREE_QUARTER_PATH = ARTIFACT_DIR / "UR_CosmicSlime_preview_three_quarter.png"
SHAPE_PREVIEW_PATHS = {
    "IdleBreath": ARTIFACT_DIR / "UR_CosmicSlime_shape_IdleBreath.png",
    "Squash": ARTIFACT_DIR / "UR_CosmicSlime_shape_Squash.png",
    "Stretch": ARTIFACT_DIR / "UR_CosmicSlime_shape_Stretch.png",
    "UltimateCollapse": ARTIFACT_DIR / "UR_CosmicSlime_shape_UltimateCollapse.png",
}


def reset_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for collection in (
        bpy.data.meshes,
        bpy.data.curves,
        bpy.data.materials,
        bpy.data.cameras,
        bpy.data.lights,
    ):
        for datablock in list(collection):
            if datablock.users == 0:
                collection.remove(datablock)


def make_material(
    name,
    base,
    metallic=0.0,
    roughness=0.48,
    emission=None,
    emission_strength=0.0,
    alpha=1.0,
):
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    material.diffuse_color = (*base, alpha)
    bsdf = material.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*base, alpha)
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    bsdf.inputs["Alpha"].default_value = alpha
    if "Coat Weight" in bsdf.inputs:
        bsdf.inputs["Coat Weight"].default_value = 0.22 if name == "MAT_Shell" else 0.05
        bsdf.inputs["Coat Roughness"].default_value = 0.32
    if emission:
        emission_input = bsdf.inputs.get("Emission Color") or bsdf.inputs.get("Emission")
        strength_input = bsdf.inputs.get("Emission Strength")
        if emission_input:
            emission_input.default_value = (*emission, 1.0)
        if strength_input:
            strength_input.default_value = emission_strength
    if alpha < 1.0:
        try:
            material.surface_render_method = "DITHERED"
        except (AttributeError, TypeError):
            pass
    return material


def assign(obj, material):
    obj.data.materials.append(material)
    return obj


def smooth(obj):
    if obj.type == "MESH":
        for polygon in obj.data.polygons:
            polygon.use_smooth = True
    return obj


def empty(name, parent=None):
    obj = bpy.data.objects.new(name, None)
    bpy.context.collection.objects.link(obj)
    obj.parent = parent
    return obj


def ico(name, location, scale, material, subdivisions=2, parent=None):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=subdivisions, radius=1.0, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    obj.parent = parent
    assign(obj, material)
    return smooth(obj)


def uv_sphere(name, location, scale, material, segments=32, rings=16, parent=None):
    bpy.ops.mesh.primitive_uv_sphere_add(
        segments=segments,
        ring_count=rings,
        radius=1.0,
        location=location,
    )
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    obj.parent = parent
    assign(obj, material)
    return smooth(obj)


def radial_slime(name, profile, top, material, segments=64, lobe_count=10):
    vertices = []
    faces = []
    ring_count = len(profile)

    for ring_index, (z, radius_x, radius_y, lobe_amount, offset_x, offset_y) in enumerate(profile):
        phase = 0.28 + ring_index * 0.045
        for index in range(segments):
            angle = math.tau * index / segments
            scallop = 1.0 + lobe_amount * math.cos(lobe_count * angle + phase)
            organic = 1.0 + 0.022 * math.sin(3.0 * angle - 0.55) + 0.010 * math.cos(5.0 * angle + 0.2)
            x = offset_x + radius_x * scallop * organic * math.cos(angle)
            y = offset_y + radius_y * scallop * (2.0 - organic) * math.sin(angle)
            vertices.append((x, y, z))

    for ring_index in range(ring_count - 1):
        for index in range(segments):
            next_index = (index + 1) % segments
            a = ring_index * segments + index
            b = ring_index * segments + next_index
            c = (ring_index + 1) * segments + next_index
            d = (ring_index + 1) * segments + index
            faces.append((a, b, c, d))

    bottom_index = len(vertices)
    vertices.append((profile[0][4], profile[0][5], 0.0))
    for index in range(segments):
        faces.append((bottom_index, (index + 1) % segments, index))

    top_index = len(vertices)
    vertices.append(top)
    last_ring = (ring_count - 1) * segments
    for index in range(segments):
        faces.append((last_ring + index, last_ring + (index + 1) % segments, top_index))

    mesh = bpy.data.meshes.new(f"{name}_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    assign(obj, material)
    return smooth(obj)


def add_slime_shape_keys(obj, nominal_height):
    basis = obj.shape_key_add(name="Basis")
    basis.interpolation = "KEY_BSPLINE"
    base_positions = [vertex.co.copy() for vertex in basis.data]

    idle = obj.shape_key_add(name="IdleBreath")
    squash = obj.shape_key_add(name="Squash")
    stretch = obj.shape_key_add(name="Stretch")
    collapse = obj.shape_key_add(name="UltimateCollapse")
    for key in (idle, squash, stretch, collapse):
        key.interpolation = "KEY_BSPLINE"
        key.slider_min = 0.0
        key.slider_max = 1.0
        key.value = 0.0

    for index, source in enumerate(base_positions):
        x, y, z = source
        height = max(0.0, min(1.0, z / nominal_height))
        grounded = max(0.0, min(1.0, (z - 0.015) / 0.15))
        belly = math.sin(math.pi * height)

        idle_scale = 1.0 + 0.028 * belly * grounded
        idle.data[index].co = (
            x * idle_scale + 0.018 * height * height,
            y * idle_scale,
            z + (0.030 * belly + 0.020 * height) * grounded,
        )

        squash_scale = 1.0 + 0.17 * grounded * (0.45 + 0.55 * belly)
        squash.data[index].co = (
            x * squash_scale - 0.025 * height,
            y * (1.0 + 0.13 * grounded),
            0.012 + (z - 0.012) * (1.0 - 0.31 * grounded),
        )

        stretch_scale = 1.0 - 0.145 * grounded * (0.55 + 0.45 * belly)
        stretch.data[index].co = (
            x * stretch_scale + 0.032 * height,
            y * stretch_scale,
            0.012 + (z - 0.012) * (1.0 + 0.24 * grounded),
        )

        collapse_weight = 0.32 + 0.68 * grounded
        target_z = 0.55 + (z - nominal_height * 0.48) * 0.34
        collapse.data[index].co = (
            x * (1.0 - 0.61 * collapse_weight),
            y * (1.0 - 0.58 * collapse_weight) - 0.035 * collapse_weight,
            z * (1.0 - collapse_weight) + target_z * collapse_weight,
        )


def catmull_rom(points, samples_per_segment=5):
    control = [Vector(points[0])] + [Vector(point) for point in points] + [Vector(points[-1])]
    sampled = []
    for index in range(1, len(control) - 2):
        p0, p1, p2, p3 = control[index - 1:index + 3]
        for step in range(samples_per_segment):
            t = step / samples_per_segment
            t2 = t * t
            t3 = t2 * t
            sampled.append(
                0.5
                * (
                    2.0 * p1
                    + (-p0 + p2) * t
                    + (2.0 * p0 - 5.0 * p1 + 4.0 * p2 - p3) * t2
                    + (-p0 + 3.0 * p1 - 3.0 * p2 + p3) * t3
                )
            )
    sampled.append(Vector(points[-1]))
    return sampled


def tapered_tube(
    name,
    control_points,
    start_radius,
    end_radius,
    material,
    radial_segments=12,
    flatten=1.0,
    parent=None,
):
    centers = catmull_rom(control_points)
    vertices = []
    faces = []
    total = max(1, len(centers) - 1)

    for index, center in enumerate(centers):
        if index == 0:
            tangent = centers[1] - center
        elif index == len(centers) - 1:
            tangent = center - centers[index - 1]
        else:
            tangent = centers[index + 1] - centers[index - 1]
        tangent.normalize()
        reference = Vector((0.0, 1.0, 0.0))
        if abs(tangent.dot(reference)) > 0.9:
            reference = Vector((1.0, 0.0, 0.0))
        side = tangent.cross(reference).normalized()
        binormal = tangent.cross(side).normalized()
        progress = index / total
        radius = end_radius + (start_radius - end_radius) * pow(1.0 - progress, 0.72)
        for ring_index in range(radial_segments):
            angle = math.tau * ring_index / radial_segments
            offset = side * (math.cos(angle) * radius)
            offset += binormal * (math.sin(angle) * radius * flatten)
            vertices.append(tuple(center + offset))

    for ring in range(len(centers) - 1):
        for index in range(radial_segments):
            next_index = (index + 1) % radial_segments
            a = ring * radial_segments + index
            b = ring * radial_segments + next_index
            c = (ring + 1) * radial_segments + next_index
            d = (ring + 1) * radial_segments + index
            faces.append((a, b, c, d))

    start_cap = len(vertices)
    vertices.append(tuple(centers[0]))
    end_cap = len(vertices)
    vertices.append(tuple(centers[-1]))
    last_ring = (len(centers) - 1) * radial_segments
    for index in range(radial_segments):
        next_index = (index + 1) % radial_segments
        faces.append((start_cap, index, next_index))
        faces.append((end_cap, last_ring + next_index, last_ring + index))

    mesh = bpy.data.meshes.new(f"{name}_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.parent = parent
    assign(obj, material)
    return smooth(obj)


def prism(name, points_xz, front_y, depth, material, parent=None):
    count = len(points_xz)
    vertices = [(x, front_y, z) for x, z in points_xz]
    vertices += [(x, front_y + depth, z) for x, z in points_xz]
    faces = [tuple(reversed(range(count))), tuple(range(count, count * 2))]
    for index in range(count):
        next_index = (index + 1) % count
        faces.append((index, index + count, next_index + count, next_index))
    mesh = bpy.data.meshes.new(f"{name}_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.parent = parent
    assign(obj, material)
    return obj


def diamond(name, center_x, center_y, center_z, width, height, depth, material, parent=None):
    points = [
        (center_x, center_z + height),
        (center_x + width, center_z),
        (center_x, center_z - height),
        (center_x - width, center_z),
    ]
    return prism(name, points, center_y, depth, material, parent)


def poly_curve(name, points, bevel_depth, material, parent=None, bevel_resolution=2):
    curve = bpy.data.curves.new(f"{name}_Curve", "CURVE")
    curve.dimensions = "3D"
    curve.resolution_u = 2
    curve.bevel_depth = bevel_depth
    curve.bevel_resolution = bevel_resolution
    curve.resolution_u = 2
    curve.use_fill_caps = True
    spline = curve.splines.new("POLY")
    spline.points.add(len(points) - 1)
    for index, point in enumerate(points):
        spline.points[index].co = (*point, 1.0)
    obj = bpy.data.objects.new(name, curve)
    bpy.context.collection.objects.link(obj)
    obj.parent = parent
    assign(obj, material)
    return obj


def face_spiral(
    name,
    center,
    start_radius,
    end_radius,
    start_angle,
    end_angle,
    bevel,
    material,
    parent=None,
):
    points = []
    steps = 40
    for index in range(steps):
        progress = index / (steps - 1)
        angle = math.radians(start_angle + (end_angle - start_angle) * progress)
        radius = start_radius + (end_radius - start_radius) * progress
        points.append(
            (
                center[0] + radius * math.cos(angle),
                center[1],
                center[2] + radius * math.sin(angle),
            )
        )
    return poly_curve(name, points, bevel, material, parent)


def ribbon_segments(
    name,
    radius_x,
    radius_y,
    center_y,
    center_z,
    angle_segments,
    tilt_x,
    tilt_z,
    width,
    thickness,
    material,
    parent=None,
    radial_offsets=(0.0,),
):
    vertices = []
    faces = []
    rotation = Matrix.Rotation(math.radians(tilt_z), 4, "Z")
    rotation @= Matrix.Rotation(math.radians(tilt_x), 4, "X")
    plane_normal = (rotation @ Vector((0.0, 0.0, 1.0))).normalized()
    center = Vector((0.0, center_y, center_z))

    for radial_offset in radial_offsets:
        for start_angle, end_angle in angle_segments:
            steps = max(7, int(abs(end_angle - start_angle) / 9.0) + 1)
            base_index = len(vertices)
            for index in range(steps):
                progress = index / (steps - 1)
                angle = math.radians(start_angle + (end_angle - start_angle) * progress)
                cosine = math.cos(angle)
                sine = math.sin(angle)
                inner = Vector(
                    (
                        (radius_x + radial_offset - width * 0.5) * cosine,
                        (radius_y + radial_offset - width * 0.5) * sine,
                        0.0,
                    )
                )
                outer = Vector(
                    (
                        (radius_x + radial_offset + width * 0.5) * cosine,
                        (radius_y + radial_offset + width * 0.5) * sine,
                        0.0,
                    )
                )
                inner = center + rotation @ inner
                outer = center + rotation @ outer
                top_offset = plane_normal * (thickness * 0.5)
                vertices.extend(
                    (
                        tuple(outer + top_offset),
                        tuple(inner + top_offset),
                        tuple(outer - top_offset),
                        tuple(inner - top_offset),
                    )
                )

            for index in range(steps - 1):
                a = base_index + index * 4
                b = base_index + (index + 1) * 4
                faces.extend(
                    (
                        (a, b, b + 1, a + 1),
                        (a + 2, a + 3, b + 3, b + 2),
                        (a, a + 2, b + 2, b),
                        (a + 1, b + 1, b + 3, a + 3),
                    )
                )
            first = base_index
            last = base_index + (steps - 1) * 4
            faces.append((first, first + 1, first + 3, first + 2))
            faces.append((last, last + 2, last + 3, last + 1))

    mesh = bpy.data.meshes.new(f"{name}_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.parent = parent
    assign(obj, material)
    return obj


def star_cloud(name, stars, material, parent=None):
    vertices = []
    faces = []
    for x, y, z, size in stars:
        base = len(vertices)
        vertices.extend(
            (
                (x + size, y, z),
                (x - size, y, z),
                (x, y + size * 0.55, z),
                (x, y - size * 0.55, z),
                (x, y, z + size),
                (x, y, z - size),
            )
        )
        faces.extend(
            (
                (base + 4, base, base + 2),
                (base + 4, base + 2, base + 1),
                (base + 4, base + 1, base + 3),
                (base + 4, base + 3, base),
                (base + 5, base + 2, base),
                (base + 5, base + 1, base + 2),
                (base + 5, base + 3, base + 1),
                (base + 5, base, base + 3),
            )
        )
    mesh = bpy.data.meshes.new(f"{name}_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.parent = parent
    assign(obj, material)
    return obj


def build_character():
    shell_mat = make_material(
        "MAT_Shell",
        (0.018, 0.004, 0.052),
        roughness=0.40,
        emission=(0.028, 0.004, 0.080),
        emission_strength=0.08,
        alpha=0.92,
    )
    nebula_mat = make_material(
        "MAT_Nebula",
        (0.080, 0.012, 0.190),
        roughness=0.50,
        emission=(0.12, 0.018, 0.31),
        emission_strength=0.32,
        alpha=0.84,
    )
    energy_mat = make_material(
        "MAT_Energy",
        (0.32, 0.055, 0.58),
        roughness=0.46,
        emission=(0.44, 0.07, 0.82),
        emission_strength=0.78,
    )
    black_core_mat = make_material("MAT_BlackCore", (0.0, 0.0, 0.0), roughness=1.0)
    orbit_mat = make_material(
        "MAT_Orbit",
        (0.065, 0.018, 0.15),
        metallic=0.12,
        roughness=0.58,
        emission=(0.10, 0.020, 0.24),
        emission_strength=0.16,
    )
    orbit_trim_mat = make_material(
        "MAT_OrbitTrim",
        (0.22, 0.085, 0.025),
        metallic=0.22,
        roughness=0.58,
        emission=(0.08, 0.018, 0.004),
        emission_strength=0.04,
    )

    root = empty("CharacterRoot")

    outer_profile = [
        (0.018, 0.52, 0.40, 0.11, -0.01, 0.00),
        (0.050, 1.06, 0.76, 0.15, -0.02, 0.00),
        (0.125, 1.14, 0.82, 0.12, -0.03, -0.01),
        (0.245, 1.09, 0.83, 0.065, -0.02, -0.01),
        (0.470, 1.07, 0.82, 0.028, 0.00, -0.015),
        (0.735, 1.03, 0.79, 0.018, 0.02, -0.010),
        (0.980, 0.95, 0.72, 0.012, 0.035, -0.005),
        (1.190, 0.81, 0.63, 0.008, 0.055, 0.005),
        (1.360, 0.59, 0.48, 0.004, 0.075, 0.010),
        (1.475, 0.27, 0.30, 0.000, 0.085, 0.010),
    ]
    body = radial_slime("SlimeBody", outer_profile, (0.10, 0.01, 1.545), shell_mat)
    body.parent = root
    add_slime_shape_keys(body, 1.545)

    inner_profile = [
        (
            0.055 + (z - 0.018) * 0.91,
            radius_x * 0.86,
            radius_y * 0.86,
            lobe * 0.36,
            offset_x,
            offset_y + 0.01,
        )
        for z, radius_x, radius_y, lobe, offset_x, offset_y in outer_profile
    ]
    inner = radial_slime("NebulaInner", inner_profile, (0.09, 0.02, 1.415), nebula_mat, segments=56)
    inner.parent = root
    add_slime_shape_keys(inner, 1.415)

    for name, location, scale, rotation in (
        ("PuddleLobe_01", (-0.88, -0.26, 0.075), (0.40, 0.30, 0.075), -12.0),
        ("PuddleLobe_02", (-0.48, -0.68, 0.060), (0.36, 0.25, 0.060), 8.0),
        ("PuddleLobe_03", (0.02, -0.76, 0.055), (0.42, 0.25, 0.055), -5.0),
        ("PuddleLobe_04", (0.57, -0.63, 0.060), (0.40, 0.27, 0.060), 12.0),
        ("PuddleLobe_05", (0.96, -0.20, 0.070), (0.34, 0.28, 0.070), -10.0),
    ):
        lobe = ico(name, location, scale, shell_mat, 2, root)
        lobe.rotation_euler.z = math.radians(rotation)

    tapered_tube(
        "Horn_L",
        [
            (-0.46, -0.02, 1.25),
            (-0.60, -0.02, 1.43),
            (-0.64, -0.015, 1.67),
            (-0.55, -0.01, 1.86),
            (-0.38, -0.02, 1.91),
            (-0.27, -0.04, 1.80),
        ],
        0.145,
        0.022,
        shell_mat,
        radial_segments=12,
        flatten=0.92,
        parent=root,
    )
    ico("HornBase_L", (-0.45, -0.01, 1.29), (0.18, 0.14, 0.17), shell_mat, 2, root)
    tapered_tube(
        "Horn_Center",
        [
            (-0.08, -0.06, 1.42),
            (-0.06, -0.07, 1.54),
            (-0.03, -0.08, 1.65),
            (0.00, -0.09, 1.72),
        ],
        0.070,
        0.012,
        nebula_mat,
        radial_segments=10,
        flatten=0.82,
        parent=root,
    )
    tapered_tube(
        "Horn_RightFluid",
        [
            (0.46, 0.00, 1.27),
            (0.58, -0.01, 1.42),
            (0.55, -0.015, 1.58),
            (0.66, -0.02, 1.70),
            (0.61, -0.03, 1.83),
            (0.50, -0.04, 1.91),
        ],
        0.105,
        0.015,
        nebula_mat,
        radial_segments=10,
        flatten=0.82,
        parent=root,
    )

    prism(
        "Eye_L",
        [(-0.59, 1.185), (-0.43, 1.145), (-0.22, 1.025), (-0.28, 0.950), (-0.44, 0.970), (-0.56, 1.055)],
        -0.835,
        0.012,
        energy_mat,
        root,
    )
    prism(
        "Eye_R",
        [(0.18, 1.020), (0.40, 1.145), (0.58, 1.180), (0.54, 1.050), (0.41, 0.965), (0.25, 0.950)],
        -0.835,
        0.012,
        energy_mat,
        root,
    )
    diamond("ForeheadSigil", 0.02, -0.760, 1.350, 0.070, 0.125, 0.012, energy_mat, root)
    diamond("ForeheadSigilInner", 0.02, -0.768, 1.350, 0.032, 0.064, 0.008, black_core_mat, root)

    star_cloud(
        "StarCloudPoints",
        [
            (-0.63, -0.75, 1.18, 0.022),
            (0.48, -0.73, 1.18, 0.018),
            (-0.70, -0.74, 0.70, 0.016),
            (0.68, -0.72, 0.70, 0.020),
            (-0.42, -0.79, 0.48, 0.012),
            (0.49, -0.79, 0.42, 0.014),
            (-0.16, -0.81, 0.76, 0.011),
            (0.22, -0.81, 0.88, 0.013),
            (-0.31, -0.80, 1.11, 0.010),
            (0.35, -0.78, 1.33, 0.012),
            (-0.79, -0.44, 0.93, 0.014),
            (0.80, -0.40, 0.98, 0.012),
            (-0.37, 0.58, 0.90, 0.015),
            (0.30, 0.60, 1.08, 0.012),
        ],
        energy_mat,
        root,
    )

    face_spiral("NebulaVeil_A", (0.02, -0.812, 0.61), 0.56, 0.34, 188.0, 405.0, 0.012, nebula_mat, root)
    face_spiral("NebulaVeil_B", (0.02, -0.816, 0.61), 0.49, 0.30, 18.0, 228.0, 0.008, energy_mat, root)

    accretion_rig = empty("SingularityAccretionRig", root)
    accretion_rig.location = (0.02, -0.900, 0.55)
    uv_sphere(
        "SingularityCore",
        (0.0, 0.0, 0.0),
        (0.285, 0.125, 0.285),
        black_core_mat,
        segments=32,
        rings=16,
        parent=accretion_rig,
    )
    bpy.ops.mesh.primitive_torus_add(
        major_segments=56,
        minor_segments=10,
        location=(0.0, 0.0, 0.0),
        rotation=(math.radians(90.0), 0.0, math.radians(14.0)),
        major_radius=0.345,
        minor_radius=0.042,
    )
    accretion = bpy.context.object
    accretion.name = "SingularityAccretion"
    accretion.scale.x = 1.15
    accretion.parent = accretion_rig
    assign(accretion, energy_mat)
    smooth(accretion)
    for index, values in enumerate(
        (
            (0.45, 0.22, 198.0, 505.0, 0.016),
            (0.38, 0.18, 30.0, 330.0, 0.011),
            (0.31, 0.15, 135.0, 425.0, 0.008),
        ),
        start=1,
    ):
        spiral = face_spiral(
            f"AccretionSpiral_{index:02d}",
            (0.0, -0.010 - index * 0.002, 0.0),
            values[0],
            values[1],
            values[2],
            values[3],
            values[4],
            energy_mat,
            accretion_rig,
        )
        spiral.scale.x = 1.12
        spiral.rotation_euler.z = math.radians(14.0)

    lower_rig = empty("OrbitRig_Lower", root)
    lower_segments = ((12.0, 102.0), (132.0, 218.0), (252.0, 346.0))
    ribbon_segments(
        "OrbitBand_Lower",
        1.12,
        0.84,
        0.01,
        0.15,
        lower_segments,
        4.0,
        3.0,
        0.085,
        0.035,
        orbit_mat,
        lower_rig,
    )
    ribbon_segments(
        "OrbitTrim_Lower",
        1.12,
        0.84,
        0.01,
        0.15,
        lower_segments,
        4.0,
        3.0,
        0.015,
        0.040,
        orbit_trim_mat,
        lower_rig,
        radial_offsets=(-0.045, 0.045),
    )

    upper_rig = empty("OrbitRig_Upper", root)
    upper_segments = ((28.0, 112.0), (145.0, 222.0), (260.0, 336.0))
    ribbon_segments(
        "OrbitBand_Upper",
        0.98,
        0.72,
        0.12,
        1.29,
        upper_segments,
        15.0,
        -7.0,
        0.072,
        0.032,
        orbit_mat,
        upper_rig,
    )
    ribbon_segments(
        "OrbitTrim_Upper",
        0.98,
        0.72,
        0.12,
        1.29,
        upper_segments,
        15.0,
        -7.0,
        0.013,
        0.037,
        orbit_trim_mat,
        upper_rig,
        radial_offsets=(-0.038, 0.038),
    )

    for name, location, scale in (
        ("CosmicDroplet_01", (-1.08, -0.01, 0.38), (0.050, 0.045, 0.095)),
        ("CosmicDroplet_02", (1.10, 0.04, 0.48), (0.045, 0.040, 0.082)),
        ("CosmicDroplet_03", (-0.88, 0.01, 1.06), (0.032, 0.029, 0.065)),
        ("CosmicDroplet_04", (0.89, 0.02, 1.24), (0.038, 0.032, 0.074)),
    ):
        ico(name, location, scale, energy_mat, 2, root)

    for name, position in (
        ("RightHandSocket", (0.78, -0.08, 0.70)),
        ("LeftHandSocket", (-0.78, -0.08, 0.70)),
        ("SkillVfxSocket", (0.02, -0.90, 0.55)),
        ("ProjectileSocket", (0.02, -0.96, 0.68)),
        ("GroundVfxSocket", (0.0, 0.0, 0.03)),
        ("TargetSocket", (0.0, 0.0, 0.88)),
        ("HealthBarSocket", (0.0, 0.0, 2.12)),
    ):
        socket = empty(name, root)
        socket.location = position

    for obj in list(bpy.context.scene.objects):
        if obj != root and obj.parent is None:
            obj.parent = root
    return root


def convert_curves():
    for obj in list(bpy.context.scene.objects):
        if obj.type != "CURVE":
            continue
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        bpy.ops.object.convert(target="MESH")
        obj.select_set(False)


def save_and_export(root):
    BLEND_PATH.parent.mkdir(parents=True, exist_ok=True)
    FBX_PATH.parent.mkdir(parents=True, exist_ok=True)
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH))

    bpy.ops.object.select_all(action="DESELECT")
    root.select_set(True)
    for child in root.children_recursive:
        child.select_set(True)
    bpy.context.view_layer.objects.active = root
    bpy.ops.export_scene.fbx(
        filepath=str(FBX_PATH),
        use_selection=True,
        object_types={"EMPTY", "MESH"},
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL",
        bake_space_transform=True,
        axis_forward="-Z",
        axis_up="Y",
        add_leaf_bones=False,
        mesh_smooth_type="FACE",
        use_mesh_modifiers=False,
        path_mode="AUTO",
    )


def point_camera(camera, target):
    direction = Vector(target) - camera.location
    camera.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def add_area_light(name, location, energy, size, color, target=(0.0, 0.0, 0.80)):
    data = bpy.data.lights.new(name, "AREA")
    data.energy = energy
    data.shape = "DISK"
    data.size = size
    data.color = color
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    point_camera(obj, target)
    return obj


def set_shape_state(name=None):
    for object_name in ("SlimeBody", "NebulaInner"):
        obj = bpy.data.objects.get(object_name)
        if not obj or not obj.data.shape_keys:
            continue
        for key in obj.data.shape_keys.key_blocks:
            key.value = 1.0 if name and key.name == name else 0.0


def render_previews():
    ARTIFACT_DIR.mkdir(parents=True, exist_ok=True)
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 960
    scene.render.resolution_y = 960
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False
    scene.render.image_settings.color_mode = "RGBA"
    try:
        scene.view_settings.look = "AgX - Medium High Contrast"
    except TypeError:
        pass

    world = bpy.data.worlds.get("World") or bpy.data.worlds.new("World")
    scene.world = world
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.014, 0.010, 0.030, 1.0)
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.38

    camera_data = bpy.data.cameras.new("PreviewCamera")
    camera = bpy.data.objects.new("PreviewCamera", camera_data)
    bpy.context.collection.objects.link(camera)
    scene.camera = camera
    camera_data.lens = 64.0

    add_area_light("PreviewKey", (-3.4, -4.6, 4.4), 560.0, 3.0, (0.58, 0.32, 0.84))
    add_area_light("PreviewFill", (3.7, -3.0, 2.4), 250.0, 3.2, (0.40, 0.22, 0.62))
    add_area_light("PreviewRim", (0.7, 3.8, 3.4), 440.0, 2.5, (0.62, 0.20, 0.72))
    add_area_light("PreviewFace", (0.0, -3.2, 1.0), 80.0, 1.8, (0.44, 0.16, 0.70))

    bpy.ops.mesh.primitive_plane_add(size=8.0, location=(0.0, 0.0, -0.012))
    floor = bpy.context.object
    floor.name = "PreviewFloor"
    floor_mat = make_material("MAT_PreviewFloor", (0.045, 0.026, 0.085), roughness=0.72)
    assign(floor, floor_mat)

    views = (
        (PREVIEW_PATH, (0.0, -5.85, 1.42), (0.0, 0.0, 0.90)),
        (PREVIEW_SIDE_PATH, (5.85, 0.0, 1.42), (0.0, 0.0, 0.90)),
        (PREVIEW_BACK_PATH, (0.0, 5.85, 1.42), (0.0, 0.0, 0.90)),
        (PREVIEW_THREE_QUARTER_PATH, (3.15, -5.25, 2.15), (0.0, 0.0, 0.88)),
    )
    set_shape_state()
    for output_path, location, target in views:
        camera.location = location
        point_camera(camera, target)
        scene.render.filepath = str(output_path)
        bpy.ops.render.render(write_still=True)

    camera.location = (3.15, -5.25, 2.15)
    point_camera(camera, (0.0, 0.0, 0.88))
    scene.render.resolution_x = 720
    scene.render.resolution_y = 720
    for shape_name, output_path in SHAPE_PREVIEW_PATHS.items():
        set_shape_state(shape_name)
        scene.render.filepath = str(output_path)
        bpy.ops.render.render(write_still=True)
    set_shape_state()


reset_scene()
character_root = build_character()
convert_curves()
save_and_export(character_root)
render_previews()

print(f"Saved Blender source: {BLEND_PATH}")
print(f"Exported Unity FBX: {FBX_PATH}")
print(f"Rendered front preview: {PREVIEW_PATH}")
print(f"Rendered side preview: {PREVIEW_SIDE_PATH}")
print(f"Rendered back preview: {PREVIEW_BACK_PATH}")
print(f"Rendered three-quarter preview: {PREVIEW_THREE_QUARTER_PATH}")
