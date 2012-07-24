using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Gaia.Rendering;
using Gaia.Rendering.RenderViews;
using Gaia.Core;

namespace Gaia.SceneGraph.GameEntities
{
    public enum LightType
    {
        Ambient,
        Directional,
        Point,
        Spot,
    };

    public class Light : Entity
    {
        Vector4 parameters;
        Vector3 color = Vector3.One;

        LightType type = LightType.Directional;

        protected bool castsShadows = false;
        ShadowRenderView[] renderViews;
        RenderTarget2D shadowMap;
        DepthStencilBuffer dsShadowMap;

        Vector3[] frustumCornersVS = new Vector3[8];
        Vector3[] frustumCornersWS = new Vector3[8];
        Vector3[] frustumCornersLS = new Vector3[8];
        Vector3[] farFrustumCornersVS = new Vector3[4];
        Vector3[] splitFrustumCornersVS = new Vector3[8];
        Matrix[] lightViewProjectionMatrices = new Matrix[GFXShaderConstants.NUM_SPLITS];
        Vector2[] lightClipPlanes = new Vector2[GFXShaderConstants.NUM_SPLITS];
        Vector4[] lightClipPositions = new Vector4[GFXShaderConstants.NUM_SPLITS];
        float[] splitDepths = new float[GFXShaderConstants.NUM_SPLITS + 1];

        bool addedToScene = false;

        bool isMainLight = false;

        public Light() : base()
        {

        }

        public Light(LightType lightType, Vector3 color, Vector3 position, bool castsShadows)
            : base()
        {
            this.type = lightType;
            this.color = color;
            this.Transformation.SetPosition(position);
            this.castsShadows = castsShadows;
            if (castsShadows)
            {
                CreateCascadeShadows(1024);
            }
        }

        public Vector3 Color
        {
            get { return color; }
            set { color = value; }
        }

        public bool CastsShadows
        {
            get { return castsShadows; }
            set
            {
                bool oldState = castsShadows;
                castsShadows = value;
                if (castsShadows != oldState)
                {
                    if (castsShadows)
                    {
                        CreateCascadeShadows(1024);
                        if (scene != null && addedToScene)
                        {
                            for (int i = 0; i < renderViews.Length; i++)
                            {
                                scene.AddRenderView(renderViews[i]);
                            }
                        }
                    }
                    else
                    {
                        DestroyShadows();
                        if (addedToScene)
                        {
                            for (int i = 0; i < renderViews.Length; i++)
                            {
                                scene.RemoveRenderView(renderViews[i]);
                            }
                        }
                    }
                }
            }
        }

        public Vector4 Parameters
        {
            get { return parameters; }
            set 
            {
                parameters = value;
                float maxScale = Math.Max(parameters.X, parameters.Y);
                Transformation.SetScale(Vector3.One * maxScale);
            }
        }

        public LightType Type
        {
            get { return type; }
            set { type = value; }
        }

        public Texture2D GetShadowMap()
        {
            return shadowMap.GetTexture();
        }

        public Matrix[] GetModelViews()
        {
            return lightViewProjectionMatrices;
        }

        public Vector2[] GetClipPlanes()
        {
            return lightClipPlanes;
        }

        public Vector4[] GetClipPositions()
        {
            return lightClipPositions;
        }

        DepthStencilBuffer oldDepthStencil;
        Viewport oldViewPort;

        public void BeginShadowMapping()
        {
            oldDepthStencil = GFX.Device.DepthStencilBuffer;
            oldViewPort = GFX.Device.Viewport;
            GFX.Device.SetRenderTarget(0, shadowMap);
            GFX.Device.DepthStencilBuffer = dsShadowMap;
            GFX.Device.Clear(Microsoft.Xna.Framework.Graphics.Color.TransparentBlack);
        }

        public void EndShadowMapping()
        {
            GFX.Device.SetRenderTarget(0, null);
            GFX.Device.DepthStencilBuffer = oldDepthStencil;
            GFX.Device.Viewport = oldViewPort;
        }

        void CreateCascadeShadows(int shadowMapSize)
        {
            int width = shadowMapSize * GFXShaderConstants.NUM_SPLITS;
            int height = shadowMapSize;
            shadowMap = new RenderTarget2D(GFX.Device, width, height, 1, SurfaceFormat.Vector2);
            dsShadowMap = new DepthStencilBuffer(GFX.Device, width, height, GFX.Device.DepthStencilBuffer.Format);
            renderViews = new ShadowRenderView[GFXShaderConstants.NUM_SPLITS];

            for (int i = 0; i < GFXShaderConstants.NUM_SPLITS; i++)
            {
                Viewport splitViewport = new Viewport();
                splitViewport.MinDepth = 0;
                splitViewport.MaxDepth = 1;
                splitViewport.Width = shadowMapSize;
                splitViewport.Height = shadowMapSize;
                splitViewport.X = i * shadowMapSize;
                splitViewport.Y = 0;
                renderViews[i] = new ShadowRenderView(this, splitViewport, i, Matrix.Identity, Matrix.Identity, Vector3.Zero, 1.0f, 1000.0f);
            }
        }

        void DestroyShadows()
        {
            if (shadowMap != null)
            {
                shadowMap.Dispose();
                shadowMap = null;
            }
            if (dsShadowMap != null)
            {
                dsShadowMap.Dispose();
                dsShadowMap = null;
            }
        }

        void ComputeFrustum(float minZ, float maxZ, RenderView cameraRenderView, int split)
        {
            // Shorten the view frustum according to the shadow view distance
            Matrix cameraMatrix = cameraRenderView.GetWorldMatrix();

            Vector4 camPos = cameraRenderView.GetEyePosShader();

            for (int i = 0; i < 4; i++)
                splitFrustumCornersVS[i] = frustumCornersVS[i + 4] * (minZ / camPos.W);

            for (int i = 4; i < 8; i++)
                splitFrustumCornersVS[i] = frustumCornersVS[i] * (maxZ / camPos.W);

            Vector3.Transform(splitFrustumCornersVS, ref cameraMatrix, frustumCornersWS);

            // Position the shadow-caster camera so that it's looking at the centroid,
            // and backed up in the direction of the sunlight
            BoundingBox sceneBounds = scene.GetSceneDimensions();
            Vector3 sceneCenter = (sceneBounds.Min + sceneBounds.Max) * 0.5f;
            Vector3 sceneExtents = (sceneBounds.Max - sceneBounds.Min) * 0.5f;
            Vector3 lightDir = -this.Transformation.GetPosition();
            lightDir.Normalize();
            Matrix viewMatrix = Matrix.CreateLookAt(sceneCenter - (lightDir * sceneExtents.Length()), sceneCenter, new Vector3(0, 1, 0));

            // Determine the position of the frustum corners in light space
            Vector3.Transform(frustumCornersWS, ref viewMatrix, frustumCornersLS);

            // Calculate an orthographic projection by sizing a bounding box 
            // to the frustum coordinates in light space
            Vector3 mins = frustumCornersLS[0];
            Vector3 maxes = frustumCornersLS[0];
            for (int i = 0; i < 8; i++)
            {
                maxes = Vector3.Max(frustumCornersLS[i], maxes);
                mins = Vector3.Min(frustumCornersLS[i], mins);
            }

            // Create an orthographic camera for use as a shadow caster
            //const float nearClipOffset = 380.0f;
            
            float nearPlane = -maxes.Z-sceneExtents.Length();
            float farPlane = -mins.Z;

            renderViews[split].SetPosition(viewMatrix.Translation);
            renderViews[split].SetView(viewMatrix);
            renderViews[split].SetNearPlane(nearPlane);
            renderViews[split].SetFarPlane(farPlane);
            renderViews[split].SetProjection(Matrix.CreateOrthographicOffCenter(mins.X, maxes.X, mins.Y, maxes.Y, nearPlane, farPlane));
            lightViewProjectionMatrices[split] = renderViews[split].GetViewProjection();
            lightClipPositions[split] = renderViews[split].GetEyePosShader();
        }

        void UpdateCascades()
        {
            RenderView mainCamera = scene.MainCamera;

            // Get corners of the main camera's bounding frustum
            Matrix cameraTransform = mainCamera.GetWorldMatrix();
            Matrix viewMatrix = mainCamera.GetView();

            mainCamera.GetFrustum().GetCorners(frustumCornersWS);
            Vector3.Transform(frustumCornersWS, ref viewMatrix, frustumCornersVS);
            for (int i = 0; i < 4; i++)
                farFrustumCornersVS[i] = frustumCornersVS[i + 4];

            // Calculate the cascade splits.  We calculate these so that each successive
            // split is larger than the previous, giving the closest split the most amount
            // of shadow detail.  
            float N = GFXShaderConstants.NUM_SPLITS;
            float near = 1.0f;//mainCamera.GetNearPlane();
            float far = mainCamera.GetFarPlane();
            splitDepths[0] = near;
            splitDepths[GFXShaderConstants.NUM_SPLITS] = far;
            const float splitConstant = 0.99f;
            for (int i = 1; i < splitDepths.Length - 1; i++)
                splitDepths[i] = splitConstant * near * (float)Math.Pow(far / near, i / N) + (1.0f - splitConstant) * ((near + (i / N)) * (far - near));

            // Render our scene geometry to each split of the cascade
            for (int i = 0; i < GFXShaderConstants.NUM_SPLITS; i++)
            {
                float minZ = splitDepths[i];
                float maxZ = splitDepths[i + 1];

                lightClipPlanes[i].X = -splitDepths[i];
                lightClipPlanes[i].Y = -splitDepths[i + 1];

                ComputeFrustum(minZ, maxZ, mainCamera, i);
            }
        }

        public override void OnSave(System.Xml.XmlWriter writer)
        {
            base.OnSave(writer);

            writer.WriteStartAttribute("castsshadows");
            writer.WriteValue(CastsShadows);
            writer.WriteEndAttribute();

            writer.WriteStartAttribute("color");
            writer.WriteValue(ParseUtils.WriteVector3(Color));
            writer.WriteEndAttribute();

            writer.WriteStartAttribute("parameters");
            writer.WriteValue(ParseUtils.WriteVector4(Parameters));
            writer.WriteEndAttribute();

            writer.WriteStartAttribute("type");
            writer.WriteValue(type.ToString());
            writer.WriteEndAttribute();

            if (scene.MainLight == this)
            {
                writer.WriteStartAttribute("ismainlight");
                writer.WriteValue(true);
                writer.WriteEndAttribute();
            }
        }

        public override void OnLoad(System.Xml.XmlNode node)
        {
            base.OnLoad(node);
            Color = ParseUtils.ParseVector3(node.Attributes["color"].Value);
            CastsShadows = bool.Parse(node.Attributes["castsshadows"].Value);
            type = (LightType)Enum.Parse(typeof(LightType), node.Attributes["type"].Value);
            Parameters = ParseUtils.ParseVector4(node.Attributes["parameters"].Value);
            if (node.Attributes["ismainlight"] != null)
            {
                if (bool.Parse(node.Attributes["ismainlight"].Value))
                    isMainLight = true;
            }
        }

        public override void OnAdd(Scene scene)
        {
            addedToScene = true;
            if (castsShadows)
            {
                for (int i = 0; i < renderViews.Length; i++)
                {
                    scene.AddRenderView(renderViews[i]);
                }
            }

            if (isMainLight)
                scene.MainLight = this;

            base.OnAdd(scene);
        }

        public override void OnDestroy()
        {
            if (castsShadows)
            {
                for (int i = 0; i < renderViews.Length; i++)
                {
                    scene.RemoveRenderView(renderViews[i]);
                }
            }
            base.OnDestroy();
        }

        public override void OnUpdate()
        {
            if (type == LightType.Directional && castsShadows)
            {
                UpdateCascades();
            }

            base.OnUpdate();
        }

        public override void OnRender(RenderView view)
        {
            bool canRender = (view.GetFrustum().Contains(Transformation.GetBounds()) != ContainmentType.Disjoint);
            canRender |= (type == LightType.Directional || type == LightType.Ambient);
            if (canRender)
            {
                LightElementManager lightMgr = (LightElementManager)view.GetRenderElementManager(RenderPass.Light);
                if (lightMgr != null)
                {
                    switch (type)
                    {
                        case LightType.Ambient:
                            lightMgr.AmbientLights.Enqueue(this);
                            break;
                        case LightType.Directional:
                            if (castsShadows)
                                lightMgr.DirectionalShadowLights.Enqueue(this);
                            else
                                lightMgr.DirectionalLights.Enqueue(this);
                            break;
                        case LightType.Point:
                            lightMgr.PointLights.Enqueue(this);
                            break;
                        case LightType.Spot:
                            lightMgr.SpotLights.Enqueue(this);
                            break;
                    }
                }
            }
            base.OnRender(view);
        }


    }
}
