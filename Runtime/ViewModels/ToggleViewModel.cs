using Kuuasema.DataBinding;
using UnityEngine.UI;

namespace Kuuasema.DataBinding {
    public class ToggleViewModel : ViewModel<bool> {

        [ViewBind]
        protected Toggle toggle;

        protected override void Awake() {
            base.Awake();
            this.toggle.onValueChanged.AddListener(this.OnToggleChanged);
        }

        protected override void SetupView() { 
            this.toggle.isOn = this.DataModel.Value;
        }

        private void OnToggleChanged(bool value) {
            this.DataModel.SetValue(value);
            // AudioSystem.Ping();
        }

        protected override void OnValueUpdated(bool value) {
            this.toggle.isOn = value;
        }
    }
}
