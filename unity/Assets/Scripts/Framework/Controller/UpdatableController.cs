using System.Collections.Generic;
using UnityEngine;

namespace Framework.Controller
{
    public class UpdatableController<T> : BaseController<T> where T : UpdatableController<T>
    {
        [SerializeReference, SubclassSelector]
        public List<Updatable<T>> updatables = new List<Updatable<T>>();
        
        public void Start()
        {
            foreach (var updatable in updatables) updatable.Start((T)(object)this);
        }

        public void Update()
        {
            foreach (var updatable in updatables) updatable.Update((T)(object)this);
        }

        private void FixedUpdate()
        {
            foreach (var updatable in updatables) updatable.FixedUpdate((T)(object)this);
        }

        private void OnTriggerEnter(Collider other)
        {
            foreach (var updatable in updatables) updatable.OnTriggerEnter((T)(object)this, other);
        }

        public void OnDrawGizmos()
        {
            foreach (var updatable in updatables) updatable.OnDrawGizmos((T)(object)this);
        }

        public void OnDestroy()
        {
            foreach (var updatable in updatables) updatable.OnDestroy((T)(object)this);
        }
    }
}