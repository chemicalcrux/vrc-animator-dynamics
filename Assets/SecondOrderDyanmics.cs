// mostly transcribed from https://www.youtube.com/watch?v=KPoeNZZ6H4s

using System;
using UnityEngine;

namespace Pursuit.Code
{
    [Serializable]
    public abstract class SecondOrderDynamics<T>
    {
        [SerializeField] private T x0;

        [SerializeField] public float f;
        [SerializeField] public float z;
        [SerializeField] public float r;

        public float k1;
        public float k2;
        public float k3;

        public float tCrit;
        public T xp;
        public T y;
        public T yd;

        public T Position => y;
        public T Velocity => yd;

        public void Setup()
        {
            xp = x0;
            y = x0;
            yd = default;

            ComputeConstants();
        }

        private void ComputeConstants()
        {
            k1 = z / (Mathf.PI * f);
            k2 = 1 / (2 * Mathf.PI * f * (2 * Mathf.PI * f));
            k3 = r * z / (2 * Mathf.PI * f);

            tCrit = 0.8f * (Mathf.Sqrt(4 * k2 + k1 * k1) - k1);
        }

        public void Update(float t, T x)
        {
            Update(t, x, GetVelocity(x, xp, t));
        }

        public void Update(float t, T x, T xd)
        {
            ComputeConstants();
            xp = x;

            t = Mathf.Min(t, tCrit);

            Integrate(ref y, ref yd, t, x, xd);
        }

        public void SetPosition(T y)
        {
            this.y = y;
        }

        public void SetVelocity(T yd)
        {
            this.yd = yd;
        }

        protected abstract T GetVelocity(T x, T xp, float t);
        protected abstract void Integrate(ref T y, ref T yd, float t, T x, T xd);
    }

    [Serializable]
    public class SecondOrderDynamicsFloat : SecondOrderDynamics<float>
    {
        public bool expanded = false;
        
        protected override float GetVelocity(float x, float xp, float t)
        {
            return (x - xp) / t;
        }

        protected override void Integrate(ref float y, ref float yd, float t, float x, float xd)
        {
            if (expanded)
            {
                float y_change = 0;
                float yd_change = 0;

                y_change += t * yd;

                yd_change += t * x / k2;
                yd_change += t * k3 * xd / k2;
                yd_change -= t * y / k2;
                yd_change -= t * t * yd / k2;
                yd_change -= t * k1 * yd / k2;
                //
                // yd_change = t * (x + k3 * xd - y - k1 * yd) / k2;

                y = y + y_change;
                yd = yd + yd_change;
            }
            else
            {
                y += t * yd;
                yd += t * (x + k3 * xd - y - k1 * yd) / k2;   
            }
        }
    }

    [Serializable]
    public class SecondOrderDynamicsVector3 : SecondOrderDynamics<Vector3>
    {
        protected override Vector3 GetVelocity(Vector3 x, Vector3 xp, float t)
        {
            return (x - xp) / t;
        }

        protected override void Integrate(ref Vector3 y, ref Vector3 yd, float t, Vector3 x, Vector3 xd)
        {
            y += t * yd;
            yd += t * (x + k3 * xd - y - k1 * yd) / k2;
        }
    }
}