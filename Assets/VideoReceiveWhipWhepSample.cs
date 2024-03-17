#if !UNITY_WEBGL
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.WebRTC.Samples
{
    public class VideoReceiveWhipWhepSample : MonoBehaviour
    {
        public WhipWhep whep;
        
#pragma warning disable 0649
        [SerializeField] private Button callButton;
        [SerializeField] private Button hangUpButton;
        [SerializeField] private RawImage receiveImage;
        [SerializeField] private AudioSource receiveAudio;
#pragma warning restore 0649

        private void Awake()
        {
            callButton.onClick.AddListener(Call);
            hangUpButton.onClick.AddListener(HangUp);
        }

        private void Start()
        {
            callButton.interactable = true;
            hangUpButton.interactable = false;
            
            StartCoroutine(WebRTC.Update());
        }

        private void Call()
        {
            callButton.interactable = false;
            hangUpButton.interactable = true;
            
            whep.Play();
            whep.stream.OnAddTrack = e =>
            {
                switch (e.Track)
                {
                    case VideoStreamTrack video:
                        video.OnVideoReceived += tex => { receiveImage.texture = tex; };
                        break;
                    case AudioStreamTrack audioTrack:
                        receiveAudio.SetTrack(audioTrack);
                        receiveAudio.loop = true;
                        receiveAudio.Play();
                        break;
                }
            };
        }

        private void HangUp()
        {
            whep.Close();
            receiveImage.texture = null;
            receiveAudio.Stop();
            receiveAudio.clip = null;
            callButton.interactable = true;
            hangUpButton.interactable = false;
        }
    }
}
#endif
