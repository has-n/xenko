// Copyright (c) 2014-2017 Silicon Studio Corp. All rights reserved. (https://www.siliconstudio.co.jp)
// See LICENSE.md for full license information.

using System.Threading.Tasks;

using SiliconStudio.Core.Mathematics;
using SiliconStudio.Xenko.Rendering;
using SiliconStudio.Xenko.Rendering.Compositing;
using SiliconStudio.Xenko.Graphics.Regression;
using SiliconStudio.Xenko.Rendering.Colors;
using SiliconStudio.Xenko.Rendering.Lights;

namespace SiliconStudio.Xenko.Engine.Tests
{
    /// <summary>
    /// Base class for engine tests.
    /// </summary>
    public class EngineTestBase : GameTestBase
    {
        protected Scene Scene;
        protected Entity Camera;
        protected LightComponent AmbientLight;

        protected CameraComponent CameraComponent
        {
            get {  return Camera.Get<CameraComponent>(); }
            set
            {
                bool alreadyAdded = false;
                for (int i = 0; i < Camera.Components.Count; i++)
                {
                    var component = Camera.Components[i];
                    if (component == value)
                    {
                        alreadyAdded = true;
                        break;
                    }
                    if (component is CameraComponent)
                    {
                        alreadyAdded = true;
                        Camera.Components[i] = value;
                        break;
                    }
                }
                if (!alreadyAdded)
                {
                    Camera.Add(value);
                }
                value.Slot = SceneSystem.GraphicsCompositor.Cameras[0].ToSlotId();
            }
        }

        public EngineTestBase()
        {
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            SceneSystem.GraphicsCompositor = Content.Load<GraphicsCompositor>("GraphicsCompositor");
            Camera = new Entity { new CameraComponent { Slot = SceneSystem.GraphicsCompositor.Cameras[0].ToSlotId() } };

            Scene = new Scene();
            Scene.Entities.Add(Camera);

            AmbientLight = new LightComponent { Type = new LightAmbient { Color = new ColorRgbProvider(Color.White) }, Intensity = 1 };
            var ambientLight = new Entity { AmbientLight };
            Scene.Entities.Add(ambientLight);

            SceneSystem.SceneInstance = new SceneInstance(Services, Scene);
        }
    }
}
