using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace Kuuasema.DataBinding {
    public class SliderViewModel : ViewModel<float>, IPointerDownHandler, IPointerUpHandler {
        [ViewBind]
        protected Slider slider;

        protected override void Awake() {
            base.Awake();
            this.slider.onValueChanged.AddListener(this.OnSliderChanged);
        }

        protected override void SetupView() { 
            this.slider.value = this.DataModel.Value;
        }

        private void OnSliderChanged(float value) {
            this.DataModel.SetValueWithAccess(value, this);
        }

        protected override void OnValueUpdated(float value) {
            this.slider.value = value;
        }

        public void OnPointerDown(PointerEventData eventData) {
            this.DataModel.Lock(this);
        }

        public void OnPointerUp(PointerEventData eventData) {
            this.DataModel.Unlock(this);
        }
    }
}
