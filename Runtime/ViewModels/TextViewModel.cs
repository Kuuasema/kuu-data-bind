using UnityEngine;
using TMPro;
namespace Kuuasema.DataBinding {
    public class TextViewModel : ViewModel<string> {
        [ViewBind]
        [SerializeField]
        private TextMeshProUGUI text;
        public TextMeshProUGUI Text => this.text;

        protected override void SetupView() { 
            this.text.text = this.DataModel.Value;
        }

        protected override void OnValueUpdated(string value) {
            this.text.text = value;
        }
    }
}
