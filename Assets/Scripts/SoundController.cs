using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundController : MonoBehaviour
{
	[SerializeField] private AudioSource _sfxSource = default;
	[SerializeField] private AudioSource[] _bgmSources = default;

	[SerializeField] private float _fadeDuration = 1f;
	[SerializeField] private AnimationCurve _inCurve = AnimationCurve.EaseInOut(0, 1, 0, 1);
	[SerializeField] private AnimationCurve _outCurve = AnimationCurve.EaseInOut(1, 0, 1, 0);

	private int _activeBgmSourceIndex;

	public void SwitchBGM(AudioClip clip, bool loop)
	{
		StopAllCoroutines();
		StartCoroutine(Switch());

		IEnumerator Switch()
		{
			var previousSource = _bgmSources[_activeBgmSourceIndex];
			var newIndex = (_activeBgmSourceIndex + 1) % _bgmSources.Length;
			var nextSource = _bgmSources[newIndex];

			_activeBgmSourceIndex = newIndex;

			for (float t = 0f; t < _fadeDuration; t += Time.deltaTime)
			{
				previousSource.volume = _outCurve.Evaluate(t / _fadeDuration);
				yield return null;
			}

			previousSource.volume = 0f;
			nextSource.clip = clip;
			nextSource.loop = loop;
			nextSource.Play();

			for (float t = 0f; t < _fadeDuration; t += Time.deltaTime)
			{
				nextSource.volume = _inCurve.Evaluate(t / _fadeDuration);
				yield return null;
			}

			nextSource.volume = 1f;
		}
	}

	public void PlaySoundEffect(AudioClip clip)
	{
		if (clip != null)
		{
			_sfxSource.PlayOneShot(clip);
		}
	}
}
