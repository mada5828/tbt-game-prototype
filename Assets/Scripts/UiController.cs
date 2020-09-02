using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UiController : MonoBehaviour
{
	private static readonly string[] RomanNumbers = new string[] { "I", "II", "III", "IV", "V", "VI", "VII" };

	[Header("General References")]
	[SerializeField] private GameManager _gameManager = default;

	[Header("Menu References")]
	public GameObject mainMenu;
	public GameObject gameMenu;
	public GameObject pauseMenu;
	public GameObject endMenu;
	public GameObject loseMenu;
	public GameObject thankYouScreen;
	public GameObject levelClearedScreen;
	public GameObject loreDumpScreen;
	[Space]
	public GameObject playerMoveButton;
	public GameObject playerPunchButton;
	public GameObject playerShootButton;

	[Header("Fade Settings")]
	[SerializeField] private CanvasGroup _levelTitleGroup = default;
	[SerializeField] private float _levelTitleFadeInDuration = 0.5f;
	[SerializeField] private float _levelTitleHoldDuration = 0.2f;
	[SerializeField] private float _levelTitleFadeOutDuration = 0.5f;
	[Space]
	[SerializeField] private CanvasGroup _loseGroup = default;
	[SerializeField] private CanvasGroup _levelClearGroup = default;
	[SerializeField] private CanvasGroup _loreDumpGroup = default;
	[SerializeField] private float _levelClearedHoldDuration = 0.2f;
	[SerializeField] private float _thankYouHoldDuration = 0.2f;
	[SerializeField] private float _loreDumpHoldDuration = 0.2f;
	[SerializeField] private Image _fadeOverlayImage = default;
	[SerializeField] private Transform _playerControlPanelTransform = default;
	[SerializeField] private Transform _playerCancelTransform = default;

	[Header("Misc")]
	[SerializeField] private AudioClip _clickSFX = default;
	[SerializeField] private TextMeshProUGUI _messageText = default;

	[Header("Level Title & Subtitles")]
	[SerializeField] private TextMeshProUGUI _levelTitleText = default;
	[SerializeField] private TextMeshProUGUI _levelSubtitleText = default;
	[SerializeField] private string[] _levelSubtitles = default;

	public event Action<PlayerAction> onChoosePlayerAction;


	public bool isFading { get; private set; }
	public bool isShowingLevelTitle { get; private set; }

	private void Awake()
	{
		InitializeDisplaySettings();
	}

	private void DoClickAction(Action action)
	{
		PlayClickSFX();
		action();
	}

	public void PlayClickSFX()
	{
		_gameManager.soundController.PlaySoundEffect(_clickSFX);
	}

	public void StartGame()
	{
		DoClickAction(_gameManager.StartGame);
	}

	public void PauseGame()
	{
		DoClickAction(_gameManager.TryTogglePause);
	}

	public void ResumeGame()
	{
		DoClickAction(_gameManager.TryTogglePause);
	}

	public void RetryLevel()
	{
		DoClickAction(_gameManager.RestartLevel);
	}

	public void ReturnToMainMenu()
	{
		DoClickAction(_gameManager.ReturnToMainMenu);
	}

	public void ExitGame()
	{
		DoClickAction(_gameManager.ExitGame);
	}

	public void PlayerChooseMove()
	{
		PlayClickSFX();
		onChoosePlayerAction?.Invoke(PlayerAction.Move);
	}

	public void PlayerChoosePunch()
	{
		PlayClickSFX();
		onChoosePlayerAction?.Invoke(PlayerAction.Punch);
	}

	public void PlayerChooseShoot()
	{
		PlayClickSFX();
		onChoosePlayerAction?.Invoke(PlayerAction.Shoot);
	}

	public void PlayerCancel()
	{
		_gameManager.soundController.PlaySoundEffect(_clickSFX);
		onChoosePlayerAction?.Invoke(PlayerAction.Cancel);
	}

	public void ShowPlayerControlPanel()
	{
		StartCoroutine(ResizePanel(_playerControlPanelTransform, 1f));
	}

	public void HidePlayerControlPanel()
	{
		StartCoroutine(ResizePanel(_playerControlPanelTransform, 0f));
	}

	public void ShowPlayerCancelButton()
	{
		StartCoroutine(ResizePanel(_playerCancelTransform, 1f));
	}

	public void HidePlayerCancelButton()
	{
		StartCoroutine(ResizePanel(_playerCancelTransform, 0f));
	}

	IEnumerator ResizePanel(Transform panel, float targetScale)
	{
		while (Mathf.Abs(panel.localScale.x - targetScale) > 0.01f)
		{
			panel.localScale = Vector3.Lerp(panel.localScale, Vector3.one * targetScale, 20f * Time.deltaTime);
			yield return null;
		}
	}

	public void Fade(float outDuration = 0.5f, float inDuration = 0.5f, float holdDuration = 0.2f, Action onScreenFullyCovered = null, Action onFadeEnd = null)
	{
		StartCoroutine(Fade());

		IEnumerator Fade()
		{
			var baseColor = _fadeOverlayImage.color;
			var clearColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);

			_fadeOverlayImage.enabled = true;
			isFading = true;

			for (float t = 0f; t < inDuration; t += Time.deltaTime)
			{
				_fadeOverlayImage.color = clearColor + Color.black * t / inDuration;
				yield return null;
			}

			_fadeOverlayImage.color = clearColor + Color.black;
			yield return null;

			onScreenFullyCovered?.Invoke();
			for (float t = 0f; t < holdDuration; t += Time.deltaTime)
			{
				yield return null;
			}

			for (float t = 0f; t < outDuration; t += Time.deltaTime)
			{
				_fadeOverlayImage.color = clearColor + Color.black * (1f - t / outDuration);
				yield return null;
			}

			_fadeOverlayImage.color = _fadeOverlayImage.color - Color.black;
			yield return null;

			_fadeOverlayImage.enabled = false;
			isFading = false;

			onFadeEnd?.Invoke();
		}
	}

	public void ShowLoseOverlay()
	{
		StartCoroutine(Fade());

		IEnumerator Fade()
		{
			loseMenu.gameObject.SetActive(true);

			for (float t = 0f; t < 1f; t += Time.deltaTime)
			{
				_loseGroup.alpha = t;
				yield return null;
			}

			_loseGroup.alpha = 1f;
		}
	}


	public void ShowLevelTitle(Action onFinished = null)
	{
		StartCoroutine(FadeLevelTitle(onFinished));
	}

	public IEnumerator ShowLevelTitle()
	{
		yield return FadeLevelTitle();
	}

	public IEnumerator ShowLevelTitle(int level)
	{
		_levelTitleText.SetText($"Level {RomanNumbers[level]}");
		_levelSubtitleText.SetText(_levelSubtitles[level]);

		yield return FadeLevelTitle();
	}

	private IEnumerator FadeLevelTitle(Action onFinished = null)
	{
		_levelTitleGroup.gameObject.SetActive(true);
		isShowingLevelTitle = true;

		for (float t = 0f; t < _levelTitleFadeInDuration; t += Time.deltaTime)
		{
			_levelTitleGroup.alpha = t / _levelTitleFadeInDuration;
			yield return null;
		}

		_levelTitleGroup.alpha = 1f;
		yield return null;

		for (float t = 0f; t < _levelTitleHoldDuration; t += Time.deltaTime)
		{
			yield return null;
		}

		for (float t = 0f; t < _levelTitleFadeOutDuration; t += Time.deltaTime)
		{
			_levelTitleGroup.alpha = 1f - t / _levelTitleFadeOutDuration;
			yield return null;
		}

		_levelTitleGroup.alpha = 0f;
		yield return null;

		_levelTitleGroup.gameObject.SetActive(false);
		isShowingLevelTitle = false;

		onFinished?.Invoke();
	}

	public IEnumerator ShowLoreDump()
	{
		loreDumpScreen.SetActive(true);
		_loreDumpGroup.alpha = 1;
		yield return new WaitForSeconds(_loreDumpHoldDuration);
		_gameManager.StartCoroutine(Hide());


		IEnumerator Hide()
		{
			for (float d = 0.5f, t = d; t > 0; t -= Time.deltaTime)
			{
				_loreDumpGroup.alpha = t / d;
				yield return null;
			}
			_loreDumpGroup.alpha = 0f;
			loreDumpScreen.SetActive(false);
		}
	}

	public void ShowThankYou()
	{
		StartCoroutine(Show());

		IEnumerator Show()
		{
			thankYouScreen.SetActive(true);
			yield return new WaitForSeconds(_thankYouHoldDuration);
			Fade(onScreenFullyCovered: () => thankYouScreen.SetActive(false));
		}
	}

	public void ShowMessage(string text)
	{
		StartCoroutine(Show());

		IEnumerator Show()
		{
			_messageText.SetText(text);
			_messageText.gameObject.SetActive(true);
			yield return new WaitForSeconds(2f);
			_messageText.gameObject.SetActive(false);
		}
	}

	public void ShowLevelCleared(Action nextAction)
	{
		StartCoroutine(Show());

		IEnumerator Show()
		{
			levelClearedScreen.SetActive(true);

			for (float t = 0f; t < 1f; t += Time.deltaTime)
			{
				_levelClearGroup.alpha = t;
				yield return null;
			}

			_levelClearGroup.alpha = 1f;

			yield return new WaitForSeconds(_levelClearedHoldDuration);
			nextAction?.Invoke();
		}
	}

	private void InitializeDisplaySettings()
	{
		if (PlayerPrefs.HasKey("display_fullscreen")) { SetDisplayFullscreen(PlayerPrefs.GetInt("display_fullscreen") == 1); }
		else { SetDisplayFullscreen(true); }

		if (!PlayerPrefs.HasKey("display_resmode")) { SetDisplayHighRes(); }
		else
		{
			switch (PlayerPrefs.GetInt("display_resmode"))
			{
				case 0: SetDisplayHighRes(); break;
				case 1: SetDisplayMidRes(); break;
				case 2: SetDisplayLowRes(); break;
			}
		}
	}

	public void ToggleDisplayFullscreen()
	{
		SetDisplayFullscreen(Screen.fullScreenMode != FullScreenMode.ExclusiveFullScreen);
	}

	public void SetDisplayFullscreen(bool fullscreen)
	{
		PlayerPrefs.SetInt("display_fullscreen", fullscreen ? 1 : 0);
		Screen.fullScreenMode = fullscreen ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.Windowed;
	}

	private void SetResolution(float multipler)
	{
		var display = Display.main;
		var w = (int)(display.systemWidth * multipler);
		var h = (int)(display.systemHeight * multipler);

		display.SetRenderingResolution(w, h);
		Screen.SetResolution(w, h, Screen.fullScreenMode);
	}

	public void SetDisplayHighRes()
	{
		PlayerPrefs.SetInt("display_resmode", 0);
		SetResolution(1f);
		Screen.SetResolution(Mathf.Max(Display.main.systemWidth, 1920), Mathf.Max(Display.main.systemHeight, 1080), Screen.fullScreenMode);
	}

	public void SetDisplayMidRes()
	{
		SetResolution(0.67f);
		Screen.SetResolution(1280, 720, Screen.fullScreenMode);
	}

	public void SetDisplayLowRes()
	{
		SetResolution(0.33f);
		Screen.SetResolution(640, 360, Screen.fullScreenMode);
	}
}
