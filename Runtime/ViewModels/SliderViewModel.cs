using Kuuasema.DataBinding;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Kuuasema.Core {
    public class SliderViewModel : ViewModel<float>, IPointerDownHandler, IPointerUpHandler {
        [ViewBind]
        private Slider slider;

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
