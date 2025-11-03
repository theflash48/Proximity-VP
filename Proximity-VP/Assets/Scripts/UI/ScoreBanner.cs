using UnityEngine;
using UnityEngine.UI;

public class ScoreBanner : MonoBehaviour
{
	public Text position;
	public Text playerName;
	public Text score;

	public void UpdateBanner(string pos, string nam, string scr) {
		position.text = pos;
		playerName.text = nam;
		score.text = scr;
	}
}
