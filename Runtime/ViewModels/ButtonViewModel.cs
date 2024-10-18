using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Kuuasema.DataBinding;
namespace Kuuasema.Core {
    public class ButtonViewModel : TextViewModel {
        [ViewBind]
        [SerializeField]
        private Button button;
        public Button Button => this.button;

        public UnityEvent<string> onButtonClicked = new UnityEvent<string>();

        protected override void SetupView() { 
            base.SetupView();

            if (this.button != null) {
                this.button.onClick.AddListener(this.OnButtonClicked);
            }
        }

        private void OnButtonClicked() {
            this.onButtonClicked.Invoke(this.DataModel.Value);
        }
        
    }
}
