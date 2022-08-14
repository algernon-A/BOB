// <copyright file="TreeAnarchyRender.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) Quistar. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace BOB.EML
{
    using System;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Runtime.CompilerServices;
    using ColossalFramework;
    using HarmonyLib;
    using TreeAnarchy;
    using UnityEngine;

    /// <summary>
    /// Copied from Tree Anarchy by Quistar.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Copied code")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1312:Variable names should begin with lower-case letter", Justification = "Copied code")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1116:Split parameters should start on line after declaration", Justification = "Copied code")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:Elements should be ordered by access", Justification = "Copied code")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1117:Parameters should be on same line or separate lines", Justification = "Copied code")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.LayoutRules", "SA1513:Closing brace should be followed by blank line", Justification = "Copied code")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony")]
    public static class TreeAnarchyRender
    {
        private static PrefabGetter<FastList<PrefabCollection<TreeInfo>.PrefabData>> m_simulationPrefabs;

        private delegate T PrefabGetter<out T>();

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Setup()
        {
            FieldInfo simPrefabs = typeof(PrefabCollection<TreeInfo>).GetField("m_simulationPrefabs", BindingFlags.NonPublic | BindingFlags.Static);
            m_simulationPrefabs = CreateGetter<FastList<PrefabCollection<TreeInfo>.PrefabData>>(simPrefabs);
        }

        private static PrefabGetter<T> CreateGetter<T>(FieldInfo field)
        {
            string methodName = field.ReflectedType.FullName + ".get_" + field.Name;
            DynamicMethod setterMethod = new DynamicMethod(methodName, typeof(T), new Type[1] { typeof(PrefabCollection<TreeInfo>) }, true);
            ILGenerator gen = setterMethod.GetILGenerator();
            if (field.IsStatic)
            {
                gen.Emit(OpCodes.Ldsfld, field);
            }
            else
            {
                gen.Emit(OpCodes.Ldarg_0);
                gen.Emit(OpCodes.Ldfld, field);
            }
            gen.Emit(OpCodes.Ret);
            return (PrefabGetter<T>)setterMethod.CreateDelegate(typeof(PrefabGetter<T>));
        }

        [HarmonyPriority(Priority.First)]
        public static unsafe bool EndRenderingImplPrefix(TreeManager __instance, RenderManager.CameraInfo cameraInfo)
        {
            int treeLayer = __instance.m_treeLayer;
            ref DrawCallData drawCallData = ref __instance.m_drawCallData;
            RenderManager rmInstance = Singleton<RenderManager>.instance;
            RenderGroup[] renderedGroups = rmInstance.m_renderedGroups.m_buffer;
            Vector4 objectIndex = RenderManager.DefaultColorLocation;
            objectIndex.z = -1f;
            int ID_Color = __instance.ID_Color;
            int ID_ObjectIndex = __instance.ID_ObjectIndex;
            int ID_TreeColor = __instance.ID_TreeColor;
            int ID_TreeLocation = __instance.ID_TreeLocation;
            int ID_TreeObjectIndex = __instance.ID_TreeObjectIndex;
            MaterialPropertyBlock materialBlock = __instance.m_materialBlock;
            Matrix4x4 identity = EMath.matrix4Identity;
            Vector3 lodMin = new Vector3(100000f, 100000f, 100000f);
            Vector3 lodMax = new Vector3(-100000f, -100000f, -100000f);
            Color black = EMath.ColorBlack;
            Vector4 v4zero = EMath.Vector4Zero;
            int len = rmInstance.m_renderedGroups.m_size;
            fixed (Quaternion* pQuaternions = &TAManager.m_treeQuaternions[0])
            fixed (TAManager.ExtraTreeInfo* pExtraInfos = &TAManager.m_extraTreeInfos[0])
            fixed (global::TreeInstance* pTrees = &__instance.m_trees.m_buffer[0])
            fixed (uint* pGrid = &__instance.m_treeGrid[0])
            {
                for (int i = 0; i < len; i++)
                {
                    RenderGroup renderGroup = renderedGroups[i];
                    if ((renderGroup.m_instanceMask & 1 << treeLayer) != 0)
                    {
                        int startX = renderGroup.m_x * 540 / 45;
                        int startZ = renderGroup.m_z * 540 / 45;
                        int endX = ((renderGroup.m_x + 1) * 540 / 45) - 1;
                        int endZ = ((renderGroup.m_z + 1) * 540 / 45) - 1;
                        for (int j = startZ; j <= endZ; j++)
                        {
                            for (int k = startX; k <= endX; k++)
                            {
                                uint treeID = *(pGrid + ((j * 540) + k));
                                while (treeID != 0u)
                                {
                                    global::TreeInstance* pTree = pTrees + treeID;
                                    ushort flags = pTree->m_flags;
                                    if ((flags & 0x0f00) != 0 && (flags & (ushort)global::TreeInstance.Flags.Hidden) == 0)
                                    {
                                        TreeInfo info = pTree->Info;
                                        Vector3 position = pTree->Position;
                                        if (info.m_prefabInitialized && cameraInfo.Intersect(position, info.m_generatedInfo.m_size.y * info.m_maxScale))
                                        {
                                            TAManager.ExtraTreeInfo* extraTreeInfo = pExtraInfos + treeID;
                                            float scale = extraTreeInfo->TreeScale;
                                            float brightness = extraTreeInfo->m_brightness;

                                            // Render overlay.
                                            RenderOverlays.HighlightTree(info, position);

                                            if (cameraInfo is null || info.m_lodMesh1 is null || EMath.CheckRenderDistance(cameraInfo, position, info.m_lodRenderDistance))
                                            {
                                                Color value = info.m_defaultColor * brightness;
                                                value.a = TAManager.GetWindSpeed(position);
                                                materialBlock.Clear();
                                                materialBlock.SetColor(ID_Color, value);
                                                materialBlock.SetVector(ID_ObjectIndex, objectIndex);
                                                drawCallData.m_defaultCalls++;
                                                Graphics.DrawMesh(info.m_mesh,
                                                    Matrix4x4.TRS(position, *(pQuaternions + ((int)((position.x * position.x) + (position.z * position.z)) % 359)), new Vector3(scale, scale, scale)),
                                                    info.m_material, info.m_prefabDataLayer, null, 0, materialBlock);
                                            }
                                            else
                                            {
                                                position.y += info.m_generatedInfo.m_center.y * (scale - 1f);
                                                Color color = info.m_defaultColor * brightness;
                                                color.a = TAManager.GetWindSpeed(position);
                                                info.m_lodLocations[info.m_lodCount] = new Vector4(position.x, position.y, position.z, scale);
                                                info.m_lodColors[info.m_lodCount] = color.linear;
                                                info.m_lodObjectIndices[info.m_lodCount] = objectIndex;
                                                info.m_lodMin = EMath.Min(info.m_lodMin, position);
                                                info.m_lodMax = EMath.Max(info.m_lodMax, position);
                                                if (++info.m_lodCount == info.m_lodLocations.Length)
                                                {
                                                    Mesh mesh;
                                                    int num;
                                                    if (info.m_lodCount <= 1)
                                                    {
                                                        mesh = info.m_lodMesh1;
                                                        num = 1;
                                                    }
                                                    else if (info.m_lodCount <= 4)
                                                    {
                                                        mesh = info.m_lodMesh4;
                                                        num = 4;
                                                    }
                                                    else if (info.m_lodCount <= 8)
                                                    {
                                                        mesh = info.m_lodMesh8;
                                                        num = 8;
                                                    }
                                                    else
                                                    {
                                                        mesh = info.m_lodMesh16;
                                                        num = 16;
                                                    }
                                                    for (int l = info.m_lodCount; l < num; l++)
                                                    {
                                                        info.m_lodLocations[l] = cameraInfo.m_forward * -100000f;
                                                        info.m_lodColors[l] = black;
                                                        info.m_lodObjectIndices[i] = v4zero;
                                                    }
                                                    materialBlock.Clear();
                                                    materialBlock.SetVectorArray(ID_TreeLocation, info.m_lodLocations);
                                                    materialBlock.SetVectorArray(ID_TreeColor, info.m_lodColors);
                                                    materialBlock.SetVectorArray(ID_TreeObjectIndex, info.m_lodObjectIndices);
                                                    Bounds bounds = default;
                                                    bounds.SetMinMax(new Vector3(info.m_lodMin.x - 100f, info.m_lodMin.y - 100f, info.m_lodMin.z - 100f),
                                                                     new Vector3(info.m_lodMax.x + 100f, info.m_lodMax.y + 100f, info.m_lodMax.z + 100f));
                                                    mesh.bounds = bounds;
                                                    info.m_lodMin = lodMin;
                                                    info.m_lodMax = lodMax;
                                                    drawCallData.m_lodCalls++;
                                                    drawCallData.m_batchedCalls += info.m_lodCount - 1;
                                                    Graphics.DrawMesh(mesh, identity, info.m_lodMaterial, info.m_prefabDataLayer, null, 0, materialBlock);
                                                    info.m_lodCount = 0;
                                                }
                                            }
                                        }
                                    }
                                    treeID = pTree->m_nextGridTree;
                                }
                            }
                        }
                    }
                }
                len = PrefabCollection<TreeInfo>.PrefabCount();
                PrefabCollection<TreeInfo>.PrefabData[] simPrefabs = m_simulationPrefabs().m_buffer;
                for (int l = 0; l < len; l++)
                {
                    if (simPrefabs[l].m_prefab is TreeInfo info && info.m_lodCount != 0)
                    {
                        Mesh mesh;
                        int num;
                        if (info.m_lodCount <= 1)
                        {
                            mesh = info.m_lodMesh1;
                            num = 1;
                        }
                        else if (info.m_lodCount <= 4)
                        {
                            mesh = info.m_lodMesh4;
                            num = 4;
                        }
                        else if (info.m_lodCount <= 8)
                        {
                            mesh = info.m_lodMesh8;
                            num = 8;
                        }
                        else
                        {
                            mesh = info.m_lodMesh16;
                            num = 16;
                        }
                        for (int i = info.m_lodCount; i < num; i++)
                        {
                            info.m_lodLocations[i] = cameraInfo.m_forward * -100000f;
                            info.m_lodColors[i] = black;
                            info.m_lodObjectIndices[i] = v4zero;
                        }
                        materialBlock.Clear();
                        materialBlock.SetVectorArray(ID_TreeLocation, info.m_lodLocations);
                        materialBlock.SetVectorArray(ID_TreeColor, info.m_lodColors);
                        materialBlock.SetVectorArray(ID_TreeObjectIndex, info.m_lodObjectIndices);
                        Bounds bounds = default;
                        bounds.SetMinMax(new Vector3(info.m_lodMin.x - 100f, info.m_lodMin.y - 100f, info.m_lodMin.z - 100f),
                                         new Vector3(info.m_lodMax.x + 100f, info.m_lodMax.y + 100f, info.m_lodMax.z + 100f));
                        mesh.bounds = bounds;
                        info.m_lodMin = lodMin;
                        info.m_lodMax = lodMax;
                        drawCallData.m_lodCalls++;
                        drawCallData.m_batchedCalls += info.m_lodCount - 1;
                        Graphics.DrawMesh(mesh, identity, info.m_lodMaterial, info.m_prefabDataLayer, null, 0, materialBlock);
                        info.m_lodCount = 0;
                    }
                }
            }
            return false;
        }
    }
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member