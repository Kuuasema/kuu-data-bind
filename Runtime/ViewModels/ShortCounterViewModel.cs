using Kuuasema.DataBinding;
using TMPro;

namespace Kuuasema.Core {
    public class ShortCounterViewModel : ViewModel<ushort> {
        [ViewBind]
        private TextMeshProUGUI text;

        protected override void SetupView() { 
            this.text.text = $"{this.DataModel.Value}";
        }

        protected override void OnValueUpdated(ushort value) {
            this.text.text = $"{value}";
        }
    }
}
