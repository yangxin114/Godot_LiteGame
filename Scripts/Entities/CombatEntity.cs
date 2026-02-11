
namespace Entities
{
    /// <summary>
    /// A base class for all combat entities.
    /// </summary>
    public partial class CombatEntity : Entity
    {
        public HealthComponent Health { get; private set; }

        public override void _Ready()
        {
            base._Ready();

            Health = new HealthComponent(this);
        }
    }
}