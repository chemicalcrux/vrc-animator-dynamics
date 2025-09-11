using System.Collections.Generic;
using Crux.AnimatorDynamics.Editor.ExtensionMethods;
using Crux.AnimatorDynamics.Runtime;
using Crux.AnimatorDynamics.Runtime.Models.SecondOrderDynamics;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

namespace Crux.AnimatorDynamics.Editor.Controls
{
    public class SecondOrderDynamicsPreview : VisualElement
    {
        private SecondOrderDynamicsModel model;
        private List<Vector2> points = new();
        
        public SecondOrderDynamicsPreview()
        {
            generateVisualContent += Paint;

            style.minHeight = 300;
        }

        public void Connect(SecondOrderDynamicsModel newModel)
        {
            model = newModel;
            Refresh();
        }

        void Refresh()
        {
            points.Clear();

            if (!model.data.TryUpgradeTo(out SecondOrderDynamicsDataV1 data))
                return;

            SecondOrderDynamicsFloat simulator = new()
            {
                f = data.f,
                z = data.z,
                r = data.r,
                x0 = data.x0
            };

            simulator.Setup();

            points.Add(new Vector2(0, simulator.Position));
            
            for (float t = 0; t < 1; t += 0.01f)
            {
                simulator.Update(0.01f, 1);
                
                points.Add(new Vector2(t, simulator.Position));
            }

            MarkDirtyRepaint();
        }

        void Paint(MeshGenerationContext ctx)
        {
            Debug.Log("Painting");
            ctx.painter2D.BeginPath();
            if (points.Count < 1)
                return;


            Vector2 xRange = new Vector2(0, 1);
            Vector2 yRange = new Vector2(-1, 2);
            
            Vector2 xRemap = new Vector2(0, resolvedStyle.width);
            Vector2 yRemap = new Vector2(resolvedStyle.height, 0);
            
            Vector2 MapPoint(Vector2 point)
            {
                return new Vector2Int(
                    (int) math.remap(xRange.x, xRange.y, xRemap.x, xRemap.y, point.x),
                    (int) math.remap(yRange.x, yRange.y, yRemap.x, yRemap.y, point.y)
                );
            }

            ctx.painter2D.lineWidth = 1;
            ctx.painter2D.strokeColor = Color.gray;
            
            for (float t = 0; t < 1; t += 0.1f)
            {
                ctx.painter2D.MoveTo(MapPoint(new Vector2(t, yRange.x)));
                ctx.painter2D.LineTo(MapPoint(new Vector2(t, yRange.y)));
            }
            
            ctx.painter2D.Stroke();
            ctx.painter2D.BeginPath();
            
            ctx.painter2D.lineWidth = 2;
            ctx.painter2D.strokeColor = Color.black;

            ctx.painter2D.MoveTo(MapPoint(points[0]));
            
            for (int idx = 1; idx < points.Count; ++idx)
            {
                ctx.painter2D.LineTo(MapPoint(points[idx]));
            }
            
            ctx.painter2D.Stroke();
        }

        public new class UxmlFactory : UxmlFactory<SecondOrderDynamicsPreview, UxmlTraits>
        {
            public override VisualElement Create(IUxmlAttributes bag, CreationContext cc)
            {
                var field = base.Create(bag, cc) as SecondOrderDynamicsPreview;

                field.TrackPropertyByName(field!.Binding, field.Refresh);

                return field;
            }
        }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            private readonly UxmlStringAttributeDescription binding = new()
            {
                name = "binding", defaultValue = ""
            };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var ate = ve as SecondOrderDynamicsPreview;

                ate!.Binding = binding.GetValueFromBag(bag, cc);
            }
        }

        public string Binding { get; set; }
    }
}