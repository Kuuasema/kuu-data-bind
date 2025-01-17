using System.Collections.Generic;
using UnityEngine;
namespace Kuuasema.DataBinding {
    public class ToggleColorViewModel : ViewModel<bool> {

        [SerializeField]
        private Color colorA;
        [SerializeField]
        private Color colorB;
        [SerializeField]
        private string shaderPropertyName;

        private MaterialPropertyBlock propertyBlock;
        [SerializeField]
        private List<Renderer> renderers = new List<Renderer>();

        protected override void SetupView() { 
            this.propertyBlock = new MaterialPropertyBlock();
            this.propertyBlock.SetColor(this.shaderPropertyName, this.DataModel.Value ? this.colorA : this.colorB);
            foreach (Renderer renderer in this.renderers) {
                renderer.SetPropertyBlock(this.propertyBlock);
            }
        }

        protected override void OnValueUpdated(bool value) {
            this.propertyBlock.SetColor(this.shaderPropertyName, value ? this.colorA : this.colorB);
            foreach (Renderer renderer in this.renderers) {
                renderer.SetPropertyBlock(this.propertyBlock);
            }
        }
    }
}
