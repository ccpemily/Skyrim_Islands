using System.Collections;
using System.Collections.Generic;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace SkyrimIslands.World
{
    public class WorldDrawLayer_SkyIslandClouds : WorldDrawLayer
    {
        private const int SubdivisionsCount = 4;
        private const float CloudAltitudeOffset = -0.8f;
        private const float CloudTimeSpeed = 2f;
        private const float OpacityChangeTime = 0.15f;

        private Material? material;
        private Texture2D? noiseTexture;
        private Texture2D? sunsetColorRamp;
        private Texture2D? cloudTexture;
        private float opacity = 1f;
        private float opacityVel;
        private bool set;

        public override bool VisibleWhenLayerNotSelected
        {
            get
            {
                return PlanetLayer.Selected != null && !PlanetLayer.Selected.IsRootSurface;
            }
        }

        public override IEnumerable Regenerate()
        {
            foreach (object item in base.Regenerate())
            {
                yield return item;
            }

            if (material == null)
            {
                material = new Material(WorldMaterials.Clouds);
                material.SetVector(ShaderPropertyIDs.Seed, new Vector2(Rand.Value, Rand.Value));
                material.SetFloat(ShaderPropertyIDs.PlanetRadius, planetLayer.Radius);
                material.SetVector(ShaderPropertyIDs.PlanetOrigin, planetLayer.Origin);
                noiseTexture = ContentFinder<Texture2D>.Get("Other/Perlin");
                sunsetColorRamp = ContentFinder<Texture2D>.Get("World/SunsetGradient");
                cloudTexture = ContentFinder<Texture2D>.Get("World/CloudMap");
                material.SetTexture(ShaderPropertyIDs.NoiseTex, noiseTexture);
                material.SetTexture(ShaderPropertyIDs.SunsetColorRamp, sunsetColorRamp);
                material.SetTexture(ShaderPropertyIDs.CloudMap, cloudTexture);
                set = false;
            }

            SphereGenerator.Generate(
                SubdivisionsCount,
                planetLayer.Radius + CloudAltitudeOffset,
                Vector3.forward,
                180f,
                out List<Vector3> verts,
                out List<int> tris);

            LayerSubMesh subMesh = GetSubMesh(material);
            subMesh.verts.AddRange(verts);
            subMesh.tris.AddRange(tris);
            FinalizeMesh(MeshParts.All);
        }

        public override void Render()
        {
            if (ShouldRegenerate)
            {
                RegenerateNow();
            }

            if (material != null)
            {
                opacity = !set ? GetTargetOpacity() : Mathf.SmoothDamp(opacity, GetTargetOpacity(), ref opacityVel, OpacityChangeTime);
                material.SetFloat(ShaderPropertyIDs.CloudShaderOpacity, opacity);
                material.SetFloat(ShaderPropertyIDs.GameTime, Find.TickManager.TicksGame / 60f * CloudTimeSpeed);
                set = true;
            }

            base.Render();
        }

        private float GetTargetOpacity()
        {
            return 1f;
        }
    }
}
