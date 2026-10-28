using Ignite.Ecs;
using Scellecs.Morpeh;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace IgniteSample.Gameplay.View
{
    /// <summary>
    /// Cube mục tiêu. Lúc được inject thì tạo entity của mình (authoring) và link vào registry.
    /// Bấm H gửi HitCommand: view không bao giờ tự trừ Health.
    /// </summary>
    [RequireComponent(typeof(EntityLink))]
    public sealed class HitTargetView : MonoBehaviour
    {
        static readonly Color Healthy = Color.white;
        static readonly Color Hurt = new Color(0.9f, 0.15f, 0.1f);

        CommandQueue<HitCommand> _hits;
        EntityLink _link;
        Renderer _renderer;
        float _flash;
        float _healthRatio = 1f;

        [Inject]
        void Construct(EcsWorldHost host, EntityViewRegistry views, CommandQueue<HitCommand> hits)
        {
            _hits = hits;
            _link = GetComponent<EntityLink>();
            _renderer = GetComponent<Renderer>();

            var entity = host.World.CreateEntity();
            host.World.GetStash<Health>().Set(entity, new Health { Current = 10f, Max = 10f });
            views.Link(entity, _link);
        }

        public void ShowDamage(float healthRatio)
        {
            _healthRatio = healthRatio;
            _flash = 0.25f;
        }

        void Update()
        {
            if (_link != null && _link.IsLinked && Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame)
                _hits.Enqueue(new HitCommand(_link.Entity, 1f));

            if (_renderer == null)
                return;
            _flash -= Time.unscaledDeltaTime;
            var settled = Color.Lerp(Hurt, Healthy, _healthRatio);
            _renderer.material.color = _flash > 0f ? Hurt : settled;
        }
    }
}
