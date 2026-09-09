using System.Collections;
using Framework.Controller;
using UnityEngine;
using UnityEngine.Events;

namespace Core.Timer
{
    public class TimerController : BaseController<TimerController>
    {
        public float CurrentTime { get; private set; }
        public float TimerDuration { get; private set; }

        public UnityAction OnTimerEnd;
        public UnityAction<float> OnTimerTick;

        private bool _isRunning;
        private bool _stopRequested;
        private Coroutine _timerCoroutine;

        public void LaunchTimer(float duration)
        {
            if (_isRunning) return;

            TimerDuration = duration;
            CurrentTime = 0f;
            _stopRequested = false;
            _isRunning = true;
            _timerCoroutine = StartCoroutine(RunTimer());
        }

        private IEnumerator RunTimer()
        {
            while (CurrentTime < TimerDuration)
            {
                if (_stopRequested) break;

                yield return null;
                CurrentTime += Time.deltaTime;
                OnTimerTick?.Invoke(CurrentTime);
            }

            bool timerCompleted = !_stopRequested && CurrentTime >= TimerDuration;
            _isRunning = false;

            if (timerCompleted)
                OnTimerEnd?.Invoke();

            CurrentTime = 0f;
            TimerDuration = 0f;
            _timerCoroutine = null;
        }

        public void StopTimer()
        {
            if (!_isRunning) return;
            _stopRequested = true;
        }

        private void OnDisable()
        {
            if (_timerCoroutine != null)
                StopCoroutine(_timerCoroutine);

            _timerCoroutine = null;
            _isRunning = false;
            _stopRequested = true;
            CurrentTime = 0f;
            TimerDuration = 0f;
        }
    }
}
