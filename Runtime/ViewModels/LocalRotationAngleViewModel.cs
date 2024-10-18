
using UnityEngine;
using Kuuasema.DataBinding;
namespace Kuuasema.Core {
    public class LocalRotationAngleViewModel : ViewModel<float> {

        protected override void OnBound() {
            this.transform.localRotation = Quaternion.Euler(0, this.DataModel.Value, 0);
            this.DataModel.OnValueUpdated += this.SetLocalRotation;
        }

        protected override void OnUnBind() {
            this.DataModel.OnValueUpdated -= this.SetLocalRotation;
        }

        private void SetLocalRotation(float degrees) { 
            this.transform.localRotation = Quaternion.Euler(0, degrees, 0);
        }
    }
}
