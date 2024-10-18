using Kuuasema.DataBinding;
using UnityEngine.UI;

namespace Kuuasema.Core {
    public class ImageFillViewModel : ViewModel<float> {
        [ViewBind]
        private Image image;
        public Image Image => this.image;

        protected override void SetupView() { 
            this.image.fillAmount = this.DataModel.Value;
        }

        protected override void OnValueUpdated(float value) {
            this.image.fillAmount = value;
        }
    }
}
