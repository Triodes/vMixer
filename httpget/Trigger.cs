using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace httpget
{
    public class Trigger
    {
        public delegate void OnTrigger();

        //method to be triggered
        OnTrigger triggeredMethod;

        //interval between triggers
        int interval;
        //counter denoting time since last trigger
        int counter = 0;

        //flag if the counter is running
        bool running = false;

        /// <param name="interval">Time between triggers in ms.</param>
        /// <param name="triggeredMethod">The method to ber triggered.</param>
        public Trigger(int interval, OnTrigger triggeredMethod)
        {
            this.interval = interval;
            this.triggeredMethod = triggeredMethod;
        }

        /// <summary>
        /// Ticks the trigger counter
        /// </summary>
        /// <param name="elapsedMilliseconds">Elapsed time since the last tick.</param>
        public void tick(int elapsedMilliseconds)
        {
            if (running)
                counter += elapsedMilliseconds;
            if (counter >= interval)
            {
                counter -= interval;
                DoTrigger();
            }
        }

        protected virtual void DoTrigger()
        {
            if (triggeredMethod != null)
                triggeredMethod();
        }

        /// <summary>
        /// Starts the trigger counter.
        /// </summary>
        public void Start()
        {
            running = true;
        }

        /// <summary>
        /// Stops the trigger counter.
        /// </summary>
        public void Stop()
        {
            running = false;
        }

        /// <summary>
        /// Stops the trigger counter and resets it to zero.
        /// </summary>
        public virtual void Reset()
        {
            Stop();
            counter = 0;
        }

        /// <summary>
        /// Gets if the counter is running.
        /// </summary>
        public bool Running
        {
            get { return running; }
        }

        public void SetInterval(int interval)
        {
            this.interval = interval;
            counter = 0;
        }
    }

    class ButtonTrigger : Trigger
    {
        public delegate void OnButtonTrigger(int button, bool longPress);

        OnButtonTrigger triggeredMethod;
        bool isTriggered = false;
        int button;
        public ButtonTrigger(int interval, OnButtonTrigger triggeredMethod, int button) 
            : base(interval, null)
        {
            this.triggeredMethod = triggeredMethod;
            this.button = button;
        }

        protected override void DoTrigger()
        {
            isTriggered = true;
            if (triggeredMethod != null)
                triggeredMethod(button, true);
            Stop();
        }

        public override void Reset()
        {
            base.Reset();
            if (triggeredMethod != null && !isTriggered)
                triggeredMethod(button, false);
            isTriggered = false;
        }
    }
}
