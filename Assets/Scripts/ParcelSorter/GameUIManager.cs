using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

namespace ParcelSorter
{
    public sealed class GameUIManager : MonoBehaviour
    {
        [Title("타이틀")]
        [SerializeField] private string creator = "[여기에 본인 이름 입력]";
        [SerializeField] private TextMeshProUGUI message;
        [SerializeField] private TextMeshProUGUI section;
        [SerializeField] private TextMeshProUGUI detail;
        [SerializeField] private Image progress;
        [SerializeField] private Button action;
        [SerializeField] private TextMeshProUGUI caption;
        [SerializeField] private PokeButton3D console;
        [SerializeField] private GameManager game;
        [SerializeField] private Transform panel;

        private void OnEnable()
        {
            action.onClick.AddListener(game.Begin);
            console.Clicked += game.Begin;
        }

        private void OnDisable()
        {
            action.onClick.RemoveListener(game.Begin);
            console.Clicked -= game.Begin;
        }

        public void ShowTitle()
        {
            section.text = "PARCEL SORTER";
            message.text = "택배 분류소";
            detail.text = $"상자를 잡고 같은 지역 칸에 놓으세요\n제작  {creator}";
            SetAction("START");
        }

        private void SetAction(string label)
        {
            panel.position = new Vector3(0f, 1.60f, 1.4f);
            panel.localScale = Vector3.one * 0.001f;
            progress.transform.parent.gameObject.SetActive(false);
            action.gameObject.SetActive(true);
            console.gameObject.SetActive(true);
            caption.text = label;
            console.Label(label);
        }

        public void ShowPlay()
        {
            action.gameObject.SetActive(false);
            console.gameObject.SetActive(false);
            panel.position = new Vector3(0f, 1.8f, 1.4f);
            panel.localScale = Vector3.one * 0.00083f;
            progress.transform.parent.gameObject.SetActive(true);
        }

        public void Refresh(int seconds, RoundScore score)
        {
            section.text = "TIME REMAINING       /       SCORE";
            message.text = $"{seconds:00}초     {score.Points}점";
            detail.text = $"정답 {score.Correct} / 처리 {score.Attempts}       콤보 {score.Combo}";
            float remaining = Mathf.Clamp01(seconds / RoundScore.Duration);
            progress.rectTransform.localScale = new Vector3(remaining, 1, 1);
        }

        public void ShowResult(RoundScore score, int best)
        {
            section.text = "ROUND COMPLETE";
            message.text = $"{score.Points}점     등급 {RoundScore.Grade(score.Points)}";
            detail.text = $"정확도 {score.Accuracy:0.#}%       최고 기록 {best}점";
            SetAction("RETRY");
        }
    }
}
