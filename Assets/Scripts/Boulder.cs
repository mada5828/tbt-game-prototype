using System.Collections;
using UnityEngine;

public class Boulder : GameEntity
{
	[SerializeField] private ParticleSystem _stateSwitchEffect = default;

	[Tooltip("The boulder will choose its visual state from this array using its current health as index")]
	[SerializeField] private GameObject[] _damageStates = default;

	public override void Initialize(GameManager gameManager, Tile tile)
	{
		base.Initialize(gameManager, tile);
		UpdateDamageState();
	}

	public override void Damage(int attackStrength)
	{
		base.Damage(attackStrength);

		ShowStateSwitchEffect();

		UpdateDamageState();
	}

	private void UpdateDamageState()
	{
		for (int i = 0; i < _damageStates.Length; i++)
		{
			_damageStates[i].SetActive(i == currentHealth);
		}
	}

	private void ShowStateSwitchEffect()
	{
		StartCoroutine(ShowEffect());

		IEnumerator ShowEffect()
		{
			_stateSwitchEffect.gameObject.SetActive(true);
			yield return new WaitWhile(_stateSwitchEffect.IsAlive);
			_stateSwitchEffect.gameObject.SetActive(false);
		}
	}
}
