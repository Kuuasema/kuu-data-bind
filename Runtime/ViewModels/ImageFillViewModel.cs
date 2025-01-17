using UnityEngine.UI;
namespace Kuuasema.DataBinding {
    public class ImageFillViewModel : ViewModel<float> {
        [ViewBind]
        protected Image image;
        public Image Image => this.image;

        protected override void SetupView() { 
            this.image.fillAmount = this.DataModel.Value;
        }

        protected override void OnValueUpdated(float value) {
            this.image.fillAmount = value;
        }
    }
}
