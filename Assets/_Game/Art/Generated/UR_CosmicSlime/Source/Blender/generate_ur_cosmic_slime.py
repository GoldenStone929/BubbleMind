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


def reset_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for collection in (bpy.data.meshes, bpy.data.curves, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
        for datablock in list(collection):
            if datablock.users == 0:
                collection.remove(datablock)


def make_material(name, base, metallic=0.0, roughness=0.35, emission=None, emission_strength=0.0, alpha=1.0):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    mat.diffuse_color = (*base, alpha)
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*base, alpha)
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    bsdf.inputs["Alpha"].default_value = alpha
    if emission:
        emission_input = bsdf.inputs.get("Emission Color") or bsdf.inputs.get("Emission")
        strength_input = bsdf.inputs.get("Emission Strength")
        if emission_input:
            emission_input.default_value = (*emission, 1.0)
        if strength_input:
            strength_input.default_value = emission_strength
    if alpha < 1.0:
        try:
            mat.surface_render_method = "DITHERED"
        except (AttributeError, TypeError):
            pass
    return mat


def assign(obj, mat):
    obj.data.materials.append(mat)
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


def ico(name, location, scale, mat, subdivisions=2):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=subdivisions, radius=1.0, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    assign(obj, mat)
    return smooth(obj)


def uv_sphere(name, location, scale, mat, segments=32, rings=16):
    bpy.ops.mesh.primitive_uv_sphere_add(
        segments=segments,
        ring_count=rings,
        radius=1.0,
        location=location,
    )
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    assign(obj, mat)
    return smooth(obj)


def radial_slime(name, profile, top_z, mat, segments=72, lobe_count=11):
    vertices = []
    faces = []
    ring_count = len(profile)

    for z, radius_x, radius_y, lobe_amount in profile:
        for index in range(segments):
            angle = math.tau * index / segments
            scallop = 1.0 + lobe_amount * math.cos(lobe_count * angle + 0.35)
            asymmetry = 1.0 + 0.018 * math.sin(3.0 * angle - 0.55)
            x = radius_x * scallop * asymmetry * math.cos(angle)
            y = radius_y * scallop * (1.0 + 0.012 * math.cos(2.0 * angle)) * math.sin(angle)
            vertices.append((x, y, z))

    for ring in range(ring_count - 1):
        for index in range(segments):
            next_index = (index + 1) % segments
            a = ring * segments + index
            b = ring * segments + next_index
            c = (ring + 1) * segments + next_index
            d = (ring + 1) * segments + index
            faces.append((a, b, c, d))

    bottom_index = len(vertices)
    vertices.append((0.0, 0.0, profile[0][0] - 0.012))
    for index in range(segments):
        faces.append((bottom_index, (index + 1) % segments, index))

    top_index = len(vertices)
    vertices.append((0.0, 0.0, top_z))
    last_ring = (ring_count - 1) * segments
    for index in range(segments):
        faces.append((last_ring + index, last_ring + (index + 1) % segments, top_index))

    mesh = bpy.data.meshes.new(f"{name}_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    assign(obj, mat)
    return smooth(obj)


def catmull_rom(points, samples_per_segment=6):
    control = [Vector(points[0])] + [Vector(point) for point in points] + [Vector(points[-1])]
    sampled = []
    for index in range(1, len(control) - 2):
        p0, p1, p2, p3 = control[index - 1:index + 3]
        for step in range(samples_per_segment):
            t = step / samples_per_segment
            t2 = t * t
            t3 = t2 * t
            point = 0.5 * (
                2.0 * p1
                + (-p0 + p2) * t
                + (2.0 * p0 - 5.0 * p1 + 4.0 * p2 - p3) * t2
                + (-p0 + 3.0 * p1 - 3.0 * p2 + p3) * t3
            )
            sampled.append(point)
    sampled.append(Vector(points[-1]))
    return sampled


def tapered_tube(name, control_points, start_radius, end_radius, mat, radial_segments=12, flatten=1.0):
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
    assign(obj, mat)
    return smooth(obj)


def prism(name, points_xz, front_y, depth, mat, bevel=0.0):
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
    assign(obj, mat)
    if bevel > 0.0:
        modifier = obj.modifiers.new("Soft bevel", "BEVEL")
        modifier.width = bevel
        modifier.segments = 2
    return obj


def diamond(name, center_x, center_y, center_z, width, height, depth, mat):
    points = [
        (center_x, center_z + height),
        (center_x + width, center_z),
        (center_x, center_z - height),
        (center_x - width, center_z),
    ]
    return prism(name, points, center_y, depth, mat, bevel=0.008)


def poly_curve(name, points, bevel_depth, mat, parent=None):
    curve = bpy.data.curves.new(f"{name}_Curve", "CURVE")
    curve.dimensions = "3D"
    curve.resolution_u = 2
    curve.bevel_depth = bevel_depth
    curve.bevel_resolution = 2
    curve.use_fill_caps = True
    spline = curve.splines.new("POLY")
    spline.points.add(len(points) - 1)
    for index, point in enumerate(points):
        spline.points[index].co = (*point, 1.0)
    obj = bpy.data.objects.new(name, curve)
    bpy.context.collection.objects.link(obj)
    obj.parent = parent
    assign(obj, mat)
    return obj


def face_spiral(name, center, start_radius, end_radius, start_angle, end_angle, bevel, mat, parent=None):
    points = []
    steps = 48
    for index in range(steps):
        progress = index / (steps - 1)
        angle = math.radians(start_angle + (end_angle - start_angle) * progress)
        radius = start_radius + (end_radius - start_radius) * progress
        x = center[0] + radius * math.cos(angle)
        z = center[2] + radius * math.sin(angle)
        points.append((x, center[1], z))
    return poly_curve(name, points, bevel, mat, parent)


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
    mat,
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
            span = abs(end_angle - start_angle)
            steps = max(8, int(span / 7.0) + 1)
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
    assign(obj, mat)
    modifier = obj.modifiers.new("Edge softness", "BEVEL")
    modifier.width = min(0.009, thickness * 0.24)
    modifier.segments = 2
    return obj


def star_cloud(name, stars, mat):
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
    assign(obj, mat)
    return obj


def build_character():
    shell_mat = make_material(
        "MAT_Shell",
        (0.0025, 0.0003, 0.009),
        roughness=0.16,
        emission=(0.018, 0.001, 0.055),
        emission_strength=0.04,
        alpha=0.82,
    )
    nebula_mat = make_material(
        "MAT_Nebula",
        (0.008, 0.0005, 0.024),
        roughness=0.42,
        emission=(0.055, 0.002, 0.15),
        emission_strength=0.20,
    )
    energy_mat = make_material(
        "MAT_Energy",
        (0.84, 0.70, 1.0),
        roughness=0.25,
        emission=(0.82, 0.34, 1.0),
        emission_strength=4.2,
    )
    black_core_mat = make_material(
        "MAT_BlackCore",
        (0.0, 0.0, 0.0),
        roughness=1.0,
    )
    orbit_mat = make_material(
        "MAT_Orbit",
        (0.032, 0.005, 0.072),
        metallic=0.72,
        roughness=0.34,
        emission=(0.10, 0.008, 0.26),
        emission_strength=0.24,
    )
    orbit_trim_mat = make_material(
        "MAT_OrbitTrim",
        (0.30, 0.16, 0.055),
        metallic=0.82,
        roughness=0.38,
        emission=(0.16, 0.045, 0.008),
        emission_strength=0.12,
    )

    root = empty("CharacterRoot")

    outer_profile = [
        (0.020, 0.60, 0.46, 0.120),
        (0.055, 1.08, 0.75, 0.160),
        (0.135, 1.10, 0.77, 0.120),
        (0.270, 0.94, 0.70, 0.055),
        (0.545, 0.84, 0.65, 0.018),
        (0.810, 0.79, 0.61, 0.010),
        (1.045, 0.70, 0.56, 0.006),
        (1.245, 0.54, 0.46, 0.000),
        (1.370, 0.31, 0.29, 0.000),
    ]
    body = radial_slime("SlimeBody", outer_profile, 1.425, shell_mat)
    body.parent = root

    inner_profile = [
        (0.065 + (z - 0.030) * 0.89, rx * 0.885, ry * 0.885, lobe * 0.34)
        for z, rx, ry, lobe in outer_profile
    ]
    inner = radial_slime("NebulaInner", inner_profile, 1.315, nebula_mat, segments=64)
    inner.parent = root

    horn_left = tapered_tube(
        "Horn_L",
        [
            (-0.46, -0.01, 1.22),
            (-0.61, -0.01, 1.43),
            (-0.68, -0.015, 1.69),
            (-0.59, -0.025, 1.91),
            (-0.40, -0.035, 2.00),
            (-0.25, -0.045, 1.90),
            (-0.25, -0.055, 1.73),
        ],
        0.145,
        0.018,
        shell_mat,
        radial_segments=14,
        flatten=0.90,
    )
    horn_left.parent = root
    ico("HornBase_L", (-0.45, -0.005, 1.25), (0.18, 0.14, 0.17), shell_mat, 2).parent = root

    center_horn = tapered_tube(
        "Horn_Center",
        [
            (-0.06, -0.025, 1.36),
            (-0.04, -0.035, 1.50),
            (-0.01, -0.045, 1.64),
            (0.01, -0.055, 1.72),
            (0.02, -0.060, 1.76),
        ],
        0.082,
        0.010,
        energy_mat,
        radial_segments=12,
        flatten=0.82,
    )
    center_horn.parent = root

    right_horn = tapered_tube(
        "Horn_RightFluid",
        [
            (0.43, 0.00, 1.25),
            (0.57, -0.01, 1.42),
            (0.54, -0.02, 1.60),
            (0.67, -0.03, 1.77),
            (0.61, -0.04, 1.94),
            (0.48, -0.05, 2.08),
            (0.54, -0.06, 2.20),
        ],
        0.115,
        0.012,
        energy_mat,
        radial_segments=12,
        flatten=0.78,
    )
    right_horn.parent = root

    left_eye = prism(
        "Eye_L",
        [
            (-0.53, 0.985),
            (-0.38, 0.955),
            (-0.18, 0.875),
            (-0.23, 0.815),
            (-0.37, 0.825),
            (-0.49, 0.875),
        ],
        -0.636,
        0.010,
        energy_mat,
        bevel=0.012,
    )
    left_eye.parent = root
    right_eye = prism(
        "Eye_R",
        [
            (0.18, 0.875),
            (0.38, 0.955),
            (0.53, 0.985),
            (0.49, 0.875),
            (0.37, 0.825),
            (0.23, 0.815),
        ],
        -0.636,
        0.010,
        energy_mat,
        bevel=0.012,
    )
    right_eye.parent = root

    sigil = diamond("ForeheadSigil", 0.0, -0.515, 1.205, 0.095, 0.185, 0.014, energy_mat)
    sigil.parent = root
    sigil_inner = diamond("ForeheadSigilInner", 0.0, -0.524, 1.205, 0.046, 0.105, 0.010, black_core_mat)
    sigil_inner.parent = root

    stars = star_cloud(
        "StarCloudPoints",
        [
            (-0.49, -0.50, 1.12, 0.025),
            (0.34, -0.53, 1.17, 0.018),
            (-0.24, -0.58, 0.69, 0.020),
            (0.46, -0.53, 0.72, 0.014),
            (-0.62, -0.37, 0.52, 0.017),
            (0.62, -0.34, 0.48, 0.021),
            (-0.52, -0.43, 0.87, 0.012),
            (0.54, -0.43, 0.94, 0.015),
            (0.12, -0.59, 0.92, 0.011),
            (-0.08, -0.56, 1.03, 0.013),
            (0.28, -0.46, 0.42, 0.012),
            (-0.31, -0.45, 0.36, 0.015),
            (0.05, 0.45, 0.86, 0.018),
            (-0.40, 0.34, 1.02, 0.014),
        ],
        energy_mat,
    )
    stars.parent = root

    veil_a = face_spiral(
        "NebulaVeil_A",
        (0.0, -0.565, 0.72),
        0.56,
        0.25,
        195.0,
        420.0,
        0.012,
        energy_mat,
    )
    veil_a.parent = root
    veil_b = face_spiral(
        "NebulaVeil_B",
        (0.02, -0.57, 0.69),
        0.46,
        0.30,
        18.0,
        238.0,
        0.008,
        energy_mat,
    )
    veil_b.parent = root

    accretion_rig = empty("SingularityAccretionRig", root)
    accretion_rig.location = (0.0, -0.705, 0.535)
    core = uv_sphere(
        "SingularityCore",
        (0.0, 0.0, 0.0),
        (0.215, 0.185, 0.215),
        black_core_mat,
        segments=32,
        rings=16,
    )
    core.parent = accretion_rig
    bpy.ops.mesh.primitive_torus_add(
        major_segments=64,
        minor_segments=12,
        location=(0.0, 0.0, 0.0),
        rotation=(math.radians(90.0), 0.0, math.radians(38.0)),
        major_radius=0.305,
        minor_radius=0.038,
    )
    accretion = bpy.context.object
    accretion.name = "SingularityAccretion"
    accretion.scale.x = 1.18
    assign(accretion, energy_mat)
    smooth(accretion)
    accretion.parent = accretion_rig
    for index, values in enumerate(
        (
            (0.43, 0.19, 205.0, 535.0, 0.020),
            (0.35, 0.14, 22.0, 330.0, 0.014),
            (0.29, 0.11, 128.0, 430.0, 0.010),
        ),
        start=1,
    ):
        spiral = face_spiral(
            f"AccretionSpiral_{index:02d}",
            (0.0, -0.012 - index * 0.002, 0.0),
            values[0],
            values[1],
            values[2],
            values[3],
            values[4],
            energy_mat,
            accretion_rig,
        )
        spiral.scale.x = 1.24
        spiral.rotation_euler.z = math.radians(38.0)

    lower_rig = empty("OrbitRig_Lower", root)
    lower_segments = ((8.0, 96.0), (116.0, 192.0), (215.0, 284.0), (305.0, 352.0))
    lower_band_width = 0.145
    ribbon_segments(
        "OrbitBand_Lower",
        1.105,
        0.825,
        0.00,
        0.20,
        lower_segments,
        5.0,
        4.0,
        lower_band_width,
        0.055,
        orbit_mat,
        lower_rig,
    )
    ribbon_segments(
        "OrbitTrim_Lower",
        1.105,
        0.825,
        0.00,
        0.20,
        lower_segments,
        5.0,
        4.0,
        0.019,
        0.064,
        orbit_trim_mat,
        lower_rig,
        radial_offsets=(lower_band_width * 0.48, -lower_band_width * 0.48),
    )

    upper_rig = empty("OrbitRig_Upper", root)
    upper_segments = ((16.0, 84.0), (108.0, 166.0), (190.0, 276.0), (300.0, 348.0))
    upper_band_width = 0.125
    ribbon_segments(
        "OrbitBand_Upper",
        0.965,
        0.715,
        0.13,
        1.04,
        upper_segments,
        18.0,
        -9.0,
        upper_band_width,
        0.050,
        orbit_mat,
        upper_rig,
    )
    ribbon_segments(
        "OrbitTrim_Upper",
        0.965,
        0.715,
        0.13,
        1.04,
        upper_segments,
        18.0,
        -9.0,
        0.017,
        0.059,
        orbit_trim_mat,
        upper_rig,
        radial_offsets=(upper_band_width * 0.48, -upper_band_width * 0.48),
    )

    droplets = (
        ("CosmicDroplet_01", (-1.03, -0.03, 0.30), (0.055, 0.045, 0.115)),
        ("CosmicDroplet_02", (1.08, 0.03, 0.43), (0.048, 0.042, 0.090)),
        ("CosmicDroplet_03", (-0.83, 0.00, 1.05), (0.038, 0.032, 0.075)),
        ("CosmicDroplet_04", (0.88, 0.02, 1.22), (0.042, 0.035, 0.085)),
        ("CosmicDroplet_05", (-0.58, 0.01, 1.48), (0.028, 0.025, 0.060)),
    )
    for name, location, scale in droplets:
        droplet = ico(name, location, scale, energy_mat, 2)
        droplet.rotation_euler.y = math.radians(18.0)
        droplet.parent = root

    for name, position in (
        ("RightHandSocket", (0.76, -0.08, 0.68)),
        ("LeftHandSocket", (-0.76, -0.08, 0.68)),
        ("SkillVfxSocket", (0.0, -0.72, 0.535)),
        ("ProjectileSocket", (0.0, -0.78, 0.70)),
        ("GroundVfxSocket", (0.0, 0.0, 0.03)),
        ("TargetSocket", (0.0, 0.0, 0.88)),
        ("HealthBarSocket", (0.0, 0.0, 2.30)),
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
        use_mesh_modifiers=True,
        path_mode="AUTO",
    )


def point_camera(camera, target):
    direction = Vector(target) - camera.location
    camera.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def add_area_light(name, location, energy, size, color):
    data = bpy.data.lights.new(name, "AREA")
    data.energy = energy
    data.shape = "DISK"
    data.size = size
    data.color = color
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    point_camera(obj, (0.0, 0.0, 0.72))
    return obj


def render_previews():
    ARTIFACT_DIR.mkdir(parents=True, exist_ok=True)
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 960
    scene.render.resolution_y = 960
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False

    world = bpy.data.worlds.get("World") or bpy.data.worlds.new("World")
    scene.world = world
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.004, 0.002, 0.011, 1.0)
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.22

    camera_data = bpy.data.cameras.new("PreviewCamera")
    camera = bpy.data.objects.new("PreviewCamera", camera_data)
    bpy.context.collection.objects.link(camera)
    scene.camera = camera
    camera_data.lens = 62.0

    add_area_light("PreviewKey", (-3.2, -4.0, 4.2), 620.0, 2.2, (0.52, 0.30, 0.88))
    add_area_light("PreviewFill", (3.7, -2.2, 2.5), 330.0, 2.4, (0.34, 0.30, 0.55))
    add_area_light("PreviewRim", (0.6, 3.4, 3.1), 520.0, 1.8, (0.78, 0.30, 0.68))

    bpy.ops.mesh.primitive_plane_add(size=8.0, location=(0.0, 0.0, -0.015))
    floor = bpy.context.object
    floor.name = "PreviewFloor"
    floor_mat = make_material("MAT_PreviewFloor", (0.007, 0.005, 0.016), roughness=0.58)
    assign(floor, floor_mat)

    views = (
        (PREVIEW_PATH, (0.0, -5.25, 1.28), (0.0, 0.0, 0.91)),
        (PREVIEW_SIDE_PATH, (5.25, 0.0, 1.28), (0.0, 0.0, 0.91)),
        (PREVIEW_BACK_PATH, (0.0, 5.25, 1.28), (0.0, 0.0, 0.91)),
        (PREVIEW_THREE_QUARTER_PATH, (2.45, -4.65, 1.76), (0.0, 0.0, 0.91)),
    )
    for output_path, location, target in views:
        camera.location = location
        point_camera(camera, target)
        scene.render.filepath = str(output_path)
        bpy.ops.render.render(write_still=True)


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
