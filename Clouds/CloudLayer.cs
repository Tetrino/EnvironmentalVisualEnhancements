﻿using Geometry;
using OverlaySystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace Clouds
{
    internal class CloudLayer
    {
        public static Dictionary<String, List<CloudLayer>> BodyDatabase = new Dictionary<string, List<CloudLayer>>();
        public static List<CloudLayer> Layers = new List<CloudLayer>();
        private static Shader GlobalCloudShader;
        private static Shader GlobalCloudParticleShader;
                
        private Material ScaledCloudMaterial;
        private Material CloudMaterial;
        private Material CloudParticleMaterial;
        private float timeDelta = 0;
        private String url;
        private ConfigNode node;
        private String body;
        private Color color;
        private float altitude;
        private TextureSet mainTexture;
        private TextureSet detailTexture;
        private ShaderFloats scaledShaderFloats;
        private ShaderFloats shaderFloats;
        private Overlay CloudOverlay;
        private VolumeManager volume = null;

        public ConfigNode ConfigNode { get { return node; } }
        public TextureSet MainTexture { get { return mainTexture; } }
        public TextureSet DetailTexture { get { return detailTexture; } }
        public Color Color { get { return color; } }
        public float Altitude { get { return altitude; } }
        public ShaderFloats ScaledShaderFloats { get { return scaledShaderFloats; } }
        public ShaderFloats ShaderFloats { get { return shaderFloats; } }
        public static Shader CloudParticleShader { get { return GlobalCloudParticleShader; } }

        internal void ApplyGUIUpdate(CloudGUI cloudGUI)
        {
            mainTexture.Clone(cloudGUI.MainTexture);
            detailTexture.Clone(cloudGUI.DetailTexture);
            scaledShaderFloats.Clone(cloudGUI.ScaledShaderFloats);
            shaderFloats.Clone(cloudGUI.ShaderFloats);
            altitude = cloudGUI.Altitude.AltitudeF;
            color = cloudGUI.Color.Color;
            UpdateTextures();
            UpdateFloats();
            CloudOverlay.UpdateAltitude(true, altitude);
            if (volume != null)
            {
                volume.Destroy();
                volume = new VolumeManager(CloudOverlay.Radius, (Texture2D)this.mainTexture.Texture, CloudParticleMaterial, this.CloudOverlay.Transform);
            }
        }

        public CloudLayer(String url, ConfigNode node, String body, Color color, float altitude,
            TextureSet mainTexture,
            TextureSet detailTexture,
            ShaderFloats ShaderFloats,
            ShaderFloats PQSShaderFloats)
        {
            if (!BodyDatabase.ContainsKey(body))
            {
                BodyDatabase.Add(body, new List<CloudLayer>());
            }
            BodyDatabase[body].Add(this);
            this.url = url;
            this.node = node;
            this.body = body;
            this.color = color;
            this.altitude = altitude;
            this.mainTexture = mainTexture;

            this.detailTexture = detailTexture;

            this.scaledShaderFloats = ShaderFloats;
            this.shaderFloats = PQSShaderFloats;
            Init();
        }

        private void UpdateTextures()
        {
            ScaledCloudMaterial.SetTexture("_MainTex", mainTexture.Texture);
            CloudMaterial.SetTexture("_MainTex", mainTexture.Texture);
            ScaledCloudMaterial.SetColor("_Color", color);
            CloudMaterial.SetColor("_Color", color);
            CloudParticleMaterial.SetColor("_Color", color);

            if (detailTexture.InUse)
            {
                ScaledCloudMaterial.SetTexture("_DetailTex", detailTexture.Texture);
                CloudMaterial.SetTexture("_DetailTex", detailTexture.Texture);
                ScaledCloudMaterial.SetFloat("_DetailScale", detailTexture.Scale);
                CloudMaterial.SetFloat("_DetailScale", detailTexture.Scale);
            }

            
        }

        public void Init()
        {
            if (GlobalCloudShader == null)
            {
                OverlayMgr.Log("Initializing Textures");
                Assembly assembly = Assembly.GetExecutingAssembly();

                StreamReader shaderStreamReader = new StreamReader(assembly.GetManifestResourceStream("Clouds.Shaders.Compiled-SphereCloud.shader"));
                
                OverlayMgr.Log("reading stream...");
                String shaderTxt = shaderStreamReader.ReadToEnd();
                GlobalCloudShader = new Material(shaderTxt).shader;
                
            }

            if (GlobalCloudParticleShader == null)
            {
                OverlayMgr.Log("Initializing Textures");
                Assembly assembly = Assembly.GetExecutingAssembly();

                StreamReader shaderStreamReader = new StreamReader(assembly.GetManifestResourceStream("Clouds.Shaders.Compiled-CloudParticle.shader"));

                OverlayMgr.Log("reading stream...");
                String shaderTxt = shaderStreamReader.ReadToEnd();
                GlobalCloudParticleShader = new Material(shaderTxt).shader;
            }

            ScaledCloudMaterial = new Material(GlobalCloudShader);
            CloudMaterial = new Material(GlobalCloudShader);
            CloudParticleMaterial = new Material(GlobalCloudParticleShader);


            scaledShaderFloats = GetDefault(true);
            shaderFloats = GetDefault(false);

            Texture2D tex1 = GameDatabase.Instance.GetTexture("BoulderCo/Clouds/Textures/particle/1", false);
            Texture2D tex2 = GameDatabase.Instance.GetTexture("BoulderCo/Clouds/Textures/particle/2", false);
            Texture2D tex3 = GameDatabase.Instance.GetTexture("BoulderCo/Clouds/Textures/particle/3", false);

            tex1.wrapMode = TextureWrapMode.Clamp;
            tex2.wrapMode = TextureWrapMode.Clamp;
            tex3.wrapMode = TextureWrapMode.Clamp;

            CloudParticleMaterial.SetTexture("_TopTex", tex1);
            CloudParticleMaterial.SetTexture("_LeftTex", tex2);
            CloudParticleMaterial.SetTexture("_FrontTex", tex3);
            CloudParticleMaterial.SetFloat("_DistFade", 1f / 2250f);

            Log("Cloud Material initialized");
            UpdateTextures();
            Log("Generating Overlay...");
            CloudOverlay = Overlay.GeneratePlanetOverlay(body, altitude, ScaledCloudMaterial, CloudMaterial, this.mainTexture.StartOffset);
            CloudOverlay.MacroCallback = MacroCallback;
            UpdateFloats();
            Log("Textures initialized");

        }

        private void MacroCallback(bool value)
        {
            if (volume != null && !value)
            {
                volume.Destroy();
                volume = null;
            }
            else if(volume == null && value)
            {
                volume = new VolumeManager(CloudOverlay.Radius, (Texture2D)this.mainTexture.Texture, CloudParticleMaterial, this.CloudOverlay.Transform);
            }
        }

        public void UpdateFloats()
        {
            if (this.scaledShaderFloats != null)
            {
                ScaledCloudMaterial.SetFloat("_FalloffPow", scaledShaderFloats.FalloffPower);
                ScaledCloudMaterial.SetFloat("_FalloffScale", scaledShaderFloats.FalloffScale);
                ScaledCloudMaterial.SetFloat("_DetailDist", scaledShaderFloats.DetailDistance);
                ScaledCloudMaterial.SetFloat("_MinLight", scaledShaderFloats.MinimumLight);
                ScaledCloudMaterial.SetFloat("_FadeDist", scaledShaderFloats.FadeDistance);
                ScaledCloudMaterial.SetFloat("_FadeScale", 0.1f/ scaledShaderFloats.FadeDistance);
            }
            else
            {
                this.scaledShaderFloats = new ShaderFloats(ScaledCloudMaterial.GetFloat("_FalloffPow"), ScaledCloudMaterial.GetFloat("_FalloffScale"), ScaledCloudMaterial.GetFloat("_DetailDist"), ScaledCloudMaterial.GetFloat("_MinLight"), ScaledCloudMaterial.GetFloat("_FadeDist"));
            }
            if (this.shaderFloats != null)
            {
                CloudMaterial.SetFloat("_FalloffPow", shaderFloats.FalloffPower);
                CloudMaterial.SetFloat("_FalloffScale", shaderFloats.FalloffScale);
                CloudMaterial.SetFloat("_DetailDist", shaderFloats.DetailDistance);
                CloudMaterial.SetFloat("_MinLight", shaderFloats.MinimumLight);
                CloudMaterial.SetFloat("_FadeDist", shaderFloats.FadeDistance);
                CloudMaterial.SetFloat("_FadeScale", 0.1f/shaderFloats.FadeDistance);
                CloudParticleMaterial.SetFloat("_MinLight", shaderFloats.MinimumLight);
            }
            else
            {
                this.shaderFloats = new ShaderFloats(CloudMaterial.GetFloat("_FalloffPow"), CloudMaterial.GetFloat("_FalloffScale"), CloudMaterial.GetFloat("_DetailDist"), CloudMaterial.GetFloat("_MinLight"), CloudMaterial.GetFloat("_FadeDist"));
            }
            
        }

        private void updateOffset(float time)
        {
            float rateOffset = time;

            mainTexture.UpdateOffset(rateOffset, true);
            CloudOverlay.UpdateRotation(mainTexture.Offset);

            if (detailTexture.InUse)
            {
                detailTexture.UpdateOffset(rateOffset, false);
                ScaledCloudMaterial.SetVector("_DetailOffset", detailTexture.Offset);
                CloudMaterial.SetVector("_DetailOffset", detailTexture.Offset);
            }

        }

        public void PerformUpdate()
        {
            timeDelta = Time.time - timeDelta;
            float timeRate = TimeWarp.CurrentRate == 0 ? 1 : TimeWarp.CurrentRate;
            float timeOffset = timeDelta * timeRate;

            updateOffset(timeOffset);

            timeDelta = Time.time;
        }

        public static void Log(String message)
        {
            UnityEngine.Debug.Log("Clouds: " + message);
        }

        public static int GetBodyLayerCount(string p)
        {
            if (BodyDatabase.ContainsKey(p))
            {
                return BodyDatabase[p].Count;
            }
            else
            {
                return 0;
            }
        }

        public static String[] GetBodyLayerStringList(string p)
        {
            if (BodyDatabase.ContainsKey(p))
            {
                int count = BodyDatabase[p].Count;
                String[] layerList = new String[count];
                for (int i = 0; i < count; i++)
                {
                    layerList[i] = "Layer " + i;
                }
                return layerList;
            }
            else
            {
                return new String[0];
            }
        }

        internal static void RemoveLayer(string body, int SelectedLayer)
        {
            if (BodyDatabase.ContainsKey(body))
            {
                CloudLayer layer = BodyDatabase[body][SelectedLayer];
                layer.node.AddValue("REMOVED", true);
                layer.Remove();
            }
            
        }

        internal static ShaderFloats GetDefault( bool isScaled)
        {
            Material compareMaterial = new Material(GlobalCloudShader);
            if (isScaled)
            {
                return new ShaderFloats(compareMaterial.GetFloat("_FalloffPow"), compareMaterial.GetFloat("_FalloffScale"), compareMaterial.GetFloat("_DetailDist"), compareMaterial.GetFloat("_MinLight"), 0.4f);
            }
            else
            {
                return new ShaderFloats(compareMaterial.GetFloat("_FalloffPow"), compareMaterial.GetFloat("_FalloffScale"), 0.000002f, compareMaterial.GetFloat("_MinLight"), 0.8f);
            }

        }

        internal static bool IsDefaultShaderFloat(ShaderFloats shaderFloats, bool isScaled)
        {
            ShaderFloats compare = GetDefault(isScaled);

            if (shaderFloats.FalloffPower == compare.FalloffPower &&
                shaderFloats.FalloffScale == compare.FalloffScale &&
                shaderFloats.DetailDistance == compare.DetailDistance &&
                shaderFloats.MinimumLight == compare.MinimumLight)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        internal void Remove(bool fromList = true)
        {
            this.CloudOverlay.RemoveOverlay();
            BodyDatabase[body].Remove(this);
            if (fromList)
            {
                Layers.Remove(this);
            }
            if (volume != null)
            {
                volume.Destroy();
                volume = null;
            }
        }

        
        internal void UpdateParticleClouds(Vector3 WorldPos)
        {
            if (volume != null)
            {
                Vector3 intendedPoint = this.CloudOverlay.Transform.InverseTransformPoint(WorldPos);
                intendedPoint.Normalize();
                volume.Update(intendedPoint);
            }
        }

    }
}
