using Microsoft.AspNetCore.Components;
using System.Diagnostics;

namespace PearlsComponents {
    public partial class Throttle<T> : ComponentBase {

        private T? oldValue = default!;

        protected override void OnParametersSet() {
            base.OnParametersSet();
            if (!EqualityComparer<T>.Default.Equals(oldValue, Value)) {
                if (!EqualityComparer<T>.Default.Equals(lastInvokedWith, Value)) {
                    _internalValue = Value;
                }
                lastInvokedWith = default;
                oldValue = Value;
            }
        }

        [Parameter]
        public T Value { get; set; } = default!;

        [Parameter]
        public EventCallback<T> ValueChanged { get; set; } = default!;

        [Parameter]
        public int Interval { get; set; } = 250;

        [Parameter]
        public bool IsThrottling { get; set; } = false;

        [Parameter]
        public EventCallback<bool> IsThrottlingChanged { get; set; } = default;

        private T _internalValue = default!;
        public T ThrottledValue {
            get => _internalValue;
            set {
                if (EqualityComparer<T>.Default.Equals(value, _internalValue)) {
                    return;
                }
                
                _internalValue = value;

                ScheduleOrTriggerChange();
            }
        }

        private Task? scheduledTask = null;
        private long lastInvokeAt = 0;
        private void ScheduleOrTriggerChange() {
            if (Stopwatch.GetElapsedTime(lastInvokeAt).TotalMilliseconds < Interval) {
                scheduledTask ??= ScheduleChange();
            } else {
                _ = TriggerChange();
            }
        }

        private async Task ScheduleChange() {
            await IsThrottlingChanged.InvokeAsync(true);
            await Task.Delay(Interval);
            await TriggerChange();
        }

        private T? lastInvokedWith = default!;
        private async Task TriggerChange() {
            if (Stopwatch.GetElapsedTime(lastInvokeAt).TotalMilliseconds < Interval) {
                return;
            }
            lastInvokeAt = Stopwatch.GetTimestamp();
            
            await InvokeAsync(async () => {
                lastInvokedWith = ThrottledValue;
                await ValueChanged.InvokeAsync(ThrottledValue);
                await IsThrottlingChanged.InvokeAsync(false);
                scheduledTask = null;
            });
        }


        [Parameter]
        public RenderFragment<Throttle<T>> ChildContent { get; set; } = default!;
    }
}