using Photon.Pun; // 유니티용 포톤 컴포넌트들
using Photon.Realtime; // 포톤 서비스 관련 라이브러리
using UnityEngine;
using UnityEngine.UI;

// 마스터(매치 메이킹) 서버와 룸 접속을 담당
public class LobbyManager : MonoBehaviourPunCallbacks {
    private string gameVersion = "1"; // 게임 버전

    public Text connectionInfoText; // 네트워크 정보를 표시할 텍스트
    public Button joinButton; // 룸 접속 버튼

    // 게임 실행과 동시에 마스터 서버 접속 시도
    private void Start() {
        PhotonNetwork.GameVersion = gameVersion;    // 접속에 필요한 정보(게임 버전) 설정

        // 설정한 정보로 마스터 서버 접속 시도
        // 마스터 서버 접속 성공 시에, OnConnectedToMaster()가 자동 실행됨.
        // 마스터 서버 접속 실패 시에, OnDisconnected()가 자동 실행됨.
        PhotonNetwork.ConnectUsingSettings();   

        joinButton.interactable = false;
        connectionInfoText.text = "Connecting Master Server.....";
    }

    // 마스터 서버 접속 성공시 자동 실행
    public override void OnConnectedToMaster() {
        joinButton.interactable = true;
        connectionInfoText.text = "Online : Connected Master Sever";       
    }

    // 마스터 서버 접속 실패시 자동 실행
    public override void OnDisconnected(DisconnectCause cause) {
        joinButton.interactable = false;
        connectionInfoText.text = "Offline : Connection failed Master Server\nTrying again Connection";

        PhotonNetwork.ConnectUsingSettings();   // 마스터 서버로 재접속 시도.
    }

    // 룸 접속 시도
    // 마스터 서버(매치메이킹 서버)를 통해 빈 무작위 룸에 접속
    public void Connect() {
        joinButton.interactable = false;    // 중복 접속 시도 방지

        if(PhotonNetwork.IsConnected){  // 접속중
            connectionInfoText.text = "Connected Room....";

            // 룸 접속 실행
            // 성공 시엔 OnJoinedRoom 이 자동 실행. 
            // 실패 시엔 OnJoinRandomFailed 가 자동 실행.
            PhotonNetwork.JoinRandomRoom(); 
        } else {
            connectionInfoText.text = "Offline : Connection failed Master Server\nTrying again Connection";
            PhotonNetwork.ConnectUsingSettings(); // 마스터 서버 재접속 시도.
        }
    }

    // (빈 룸이 없어)랜덤 룸 참가에 실패한 경우 자동 실행
    public override void OnJoinRandomFailed(short returnCode, string message) {
        connectionInfoText.text = "No Empty Room, Create New Room....";
        PhotonNetwork.CreateRoom(null, new RoomOptions{MaxPlayers = 4});    // 빈 룸 생성
    }

    // 룸에 참가 완료된 경우 자동 실행
    public override void OnJoinedRoom() {
        connectionInfoText.text = "Successful Enter Room";
        PhotonNetwork.LoadLevel("Main");    // 모든 룸 참가자가 Main 씬을 로드하게 함.
    }
}