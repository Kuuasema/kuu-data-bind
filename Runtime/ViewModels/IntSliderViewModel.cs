using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace Kuuasema.DataBinding {
    public class IntSliderViewModel : ViewModel<int>, IPointerDownHandler, IPointerUpHandler {
        [ViewBind]
        protected Slider slider;

        public virtual int MaxValue { get; protected set; } = 1;
        public virtual bool Inverse { get; protected set; }

        protected override void Awake() {
            base.Awake();
            this.slider.onValueChanged.AddListener(this.OnSliderChanged);
            this.slider.maxValue = this.MaxValue;
            this.slider.wholeNumbers = true;
        }

        protected override void SetupView() { 
            int value = this.DataModel.Value;
            if (this.Inverse) {
                value = this.MaxValue - value;
            }
            this.slider.value = value;
        }

        private void OnSliderChanged(float value) {
            int intVal = Mathf.RoundToInt(value);
            if (this.Inverse) {
                intVal = this.MaxValue - intVal;
            }
            this.DataModel.SetValueWithAccess(intVal, this);
        }

        protected override void OnValueUpdated(int value) {
            if (this.Inverse) {
                value = this.MaxValue - value;
            }
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
