﻿#define USE_DEBUG_KEY

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GreeningEx2019
{
    public class StageManager : SceneManagerBase
    {
        public static StageManager instance = null;

        [Tooltip("ゲーム用プレイヤープレハブ"), SerializeField]
        GameObject stellaPrefab = null;
        [Tooltip("クリアフェードの色"), SerializeField]
        Color clearFadeColor = Color.green;
        [Tooltip("クリア用マテリアル"), SerializeField]
        Material[] clearBGMaterials = new Material[3];
        [Tooltip("クリアテキスト"), SerializeField]
        Animator clearText = null;

        const float RollingSeconds = 0.8f;
        const float ClearFadeSeconds = 0.24f;

        /// <summary>
        /// ミスからの開始
        /// </summary>
        readonly SoundController.SeType[] missStartSerif =
        {
            SoundController.SeType.GimmickKiwotsukete,
            SoundController.SeType.GimmickDaijoubu,
        };

        readonly SoundController.SeType[] initStartSerif =
        {
            SoundController.SeType.GimmickGanbare,
            SoundController.SeType.GimmickKiwotsukete,
        };

        /// <summary>
        /// クリアアニメをする際の、ゴールからステラへの相対座標。
        /// ステラのX座標を星からの相対座標で求め、
        /// 星の高さはステラのY座標からの相対座標で求める
        /// </summary>
        public static readonly Vector3 GoalToStellaOffset = new Vector3(-0.25f, -1.95f);

        /// <summary>
        /// クリア処理中の時、true
        /// </summary>
        public static bool IsClearPlaying { get; private set; }

        /// <summary>
        /// ステージの苗の数
        /// </summary>
        public static int NaeCount { get; private set; }

        /// <summary>
        /// 操作可能な状態かどうかを返します。
        /// </summary>
        public static bool CanMove
        {
            get
            {
                return !Fade.IsFading;
            }
        }

        /// <summary>
        /// ステージの左端
        /// </summary>
        public static float StageLeft { get; private set; }
        /// <summary>
        /// ステージの右端
        /// </summary>
        public static float StageRight { get; private set; }
        /// <summary>
        /// ステージの下端
        /// </summary>
        public static float StageBottom { get; private set; }

        /// <summary>
        /// クリアできる状況ならtrueを返します。
        /// </summary>
        public static bool CanClear
        {
            get
            {
                return Grow.NaeGrowedCount >= StageManager.NaeCount;
            }
        }

        private new void Awake()
        {
            instance = this;
            IsClearPlaying = false;
            base.Awake();
        }

        /// <summary>
        /// 指定のtagのオブジェクトの左端、右端、下端を求めます。
        /// </summary>
        /// <param name="tag">タグ</param>
        void StageMinMax(string tag)
        {
            GameObject[] maps = GameObject.FindGameObjectsWithTag(tag);
            for (int i = 0; i < maps.Length; i++)
            {
                Bounds bnd = maps[i].GetComponent<Collider>().bounds;
                StageLeft = Mathf.Min(StageLeft, bnd.min.x);
                StageRight = Mathf.Max(StageRight, bnd.max.x);
                StageBottom = Mathf.Min(StageBottom, bnd.min.y);
            }
        }

        public override void OnFadeOutDone()
        {
            StageLeft = float.PositiveInfinity;
            StageRight = float.NegativeInfinity;
            StageBottom = float.PositiveInfinity;
            StageMinMax("Ground");
            StageMinMax("DeadZone");

            SoundController.PlayBGM(SoundController.BgmType.Game0, true);
            SceneManager.SetActiveScene(gameObject.scene);

            // プレイヤーを入れ替える
            GameObject stabPlayer = GameObject.FindGameObjectWithTag("Player");
            GameObject myp = Instantiate(stellaPrefab, stabPlayer.transform.position, stabPlayer.transform.rotation);
            Destroy(stabPlayer);

            // カメラにターゲットを設定
            FollowCamera fcam = Camera.main.gameObject.GetComponent<FollowCamera>();
            fcam.SetTarget(myp.transform);

            // 苗の数を数える
            Grow[] gr = GameObject.FindObjectsOfType<Grow>();
            NaeCount = gr.Length;

            Grow.Init();
        }

        public override void OnFadeInDone()
        {
            int index = Random.Range(0, 2);

            if (GameParams.isMiss)
            {
                GameParams.isMiss = false;
                SoundController.Play(missStartSerif[index]);
            }
            else
            {
                SoundController.Play(initStartSerif[index]);
            }

            base.OnFadeInDone();
        }

#if USE_DEBUG_KEY
        private void FixedUpdate()
        {
            if (Input.GetButtonDown("Esc"))
            {
                // ステージ選択へ
                GameParams.Instance.toStageSelect = StageSelectManager.ToStageSelectType.Back;
                SceneChanger.ChangeScene(SceneChanger.SceneType.StageSelect);
            }
        }
#endif

        /// <summary>
        /// クリア処理を開始
        /// </summary>
        public static void StartClear()
        {
            IsClearPlaying = true;
            instance.StartCoroutine(instance.ClearSequence());
            SoundController.PlayBGM(SoundController.BgmType.Clear);
        }

        IEnumerator ClearSequence()
        {
            // クリア表示
            clearText.SetTrigger("Show");
            StellaMove.instance.ChangeAction(StellaMove.ActionType.Clear);

            // 星を回転させる
            Goal.ClearAnim();
            yield return new WaitForSeconds(RollingSeconds);

            // フェードアウト
            Color lastColor = Fade.NowColor;
            yield return Fade.StartFade(Fade.FadeStateType.Out, clearFadeColor, ClearFadeSeconds);

            // 背景を切り替える
            BGScroller.instance.ChangeMaterials(clearBGMaterials);

            // フェードイン
            yield return Fade.StartFade(Fade.FadeStateType.In, clearFadeColor, ClearFadeSeconds);
            Fade.SetFadeColor(lastColor);

            // ステラが飛び乗る
            StellaClear.HoldStar();
        }
    }
}
