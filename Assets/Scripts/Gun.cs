using System.Collections;
using Photon.Pun;
using UnityEngine;

// 총을 구현한다
public class Gun : MonoBehaviourPun, IPunObservable {
    // 총의 상태를 표현하는데 사용할 타입을 선언한다
    public enum State {
        Ready, // 발사 준비됨
        Empty, // 탄창이 빔
        Reloading // 재장전 중
    }

    public State state { get; private set; } // 현재 총의 상태

    public Transform fireTransform; // 총알이 발사될 위치

    public ParticleSystem muzzleFlashEffect; // 총구 화염 효과
    public ParticleSystem shellEjectEffect; // 탄피 배출 효과

    private LineRenderer bulletLineRenderer; // 총알 궤적을 그리기 위한 렌더러

    private AudioSource gunAudioPlayer; // 총 소리 재생기
    public AudioClip shotClip; // 발사 소리
    public AudioClip reloadClip; // 재장전 소리

    public float damage = 25; // 공격력
    private float fireDistance = 50f; // 사정거리

    public int ammoRemain = 100; // 남은 전체 탄약
    public int magCapacity = 25; // 탄창 용량
    public int magAmmo; // 현재 탄창에 남아있는 탄약


    public float timeBetFire = 0.12f; // 총알 발사 간격
    public float reloadTime = 1.8f; // 재장전 소요 시간
    private float lastFireTime; // 총을 마지막으로 발사한 시점
    
    // 주기적으로 자동 실행되는 동기화 함수
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info){
        // 로컬 오브젝트면 wirte부분이 실행됨
        if(stream.IsWriting){
            stream.SendNext(ammoRemain);    // 남은 탄알 수를 네트워크를 통해 보냄
            stream.SendNext(magAmmo);   // 탄창의 탄알 수를 네트워크를 통해 보냄
            stream.SendNext(state); // 현재 총의 상태를 네트워크를 통해 보냄
        } else {
            // 리모트 오브젝트라면 읽기 부분 실행.
            ammoRemain = (int)stream.ReceiveNext();
            magAmmo = (int)stream.ReceiveNext();
            state = (State)stream.ReceiveNext();
        }
    }

    // 남은 탄알을 추가하는 함수
    [PunRPC]
    public void AddAmmo(int ammo){
        ammoRemain += ammo;
    }

    private void Awake() {
        // 사용할 컴포넌트들의 참조를 가져오기
        gunAudioPlayer = GetComponent<AudioSource>();
        bulletLineRenderer = GetComponent<LineRenderer>();

        bulletLineRenderer.positionCount = 2;
        bulletLineRenderer.enabled = false;
    }

    private void OnEnable() {
        // 총 상태 초기화
        magAmmo = magCapacity;
        state = State.Ready;
        lastFireTime = 0;
    }

    // 발사 시도
    public void Fire() {
        if(state == State.Ready && Time.time >= lastFireTime + timeBetFire){
            lastFireTime = Time.time;
            Shot();
        }
    }

    // 실제 발사 처리
    private void Shot() {
        // 실제 발사 처리는 호스트에 대리
        photonView.RPC("ShotProcessOnServer", RpcTarget.MasterClient);

        --magAmmo;
        if(magAmmo <= 0)
            state = State.Empty;
    }

    // 호스트에서 실행되는 실제 발사 처리
    [PunRPC]
    private void ShotProcessOnServer(){
        RaycastHit hit;
        Vector3 hitPosition  = Vector3.zero;

        if(Physics.Raycast(fireTransform.position, fireTransform.forward, out hit, fireDistance)){
            // 충돌한 상대방으로부터 IDamageable 오브젝트 가져오기
            IDamageable target = hit.collider.GetComponent<IDamageable>();
            if(target != null){
                target.OnDamage(damage, hit.point, hit.normal);
            }

            hitPosition = hit.point;
        } else {
            hitPosition = fireTransform.position + fireTransform.forward * fireDistance;
        }

        //발사 이펙트 재생. 이펙트 재생은 모든 클라이언트에서 실행.
        photonView.RPC("ShotEffectProcessOnClients", RpcTarget.All, hitPosition);
    }

    // 이펙트 재생 코루틴을 랩핑하는 메서드
    [PunRPC]
    private void ShotEffectProcessOnClients(Vector3 hitPosition){
        StartCoroutine(ShotEffect(hitPosition));
    }

    // 발사 이펙트와 소리를 재생하고 총알 궤적을 그린다
    private IEnumerator ShotEffect(Vector3 hitPosition) {
        muzzleFlashEffect.Play();
        shellEjectEffect.Play();

        gunAudioPlayer.PlayOneShot(shotClip);
        bulletLineRenderer.SetPosition(0, fireTransform.position);
        bulletLineRenderer.SetPosition(1, hitPosition);

        // 라인 렌더러를 활성화하여 총알 궤적을 그린다
        bulletLineRenderer.enabled = true;

        // 0.03초 동안 잠시 처리를 대기
        yield return new WaitForSeconds(0.03f);

        // 라인 렌더러를 비활성화하여 총알 궤적을 지운다
        bulletLineRenderer.enabled = false;
    }

    // 재장전 시도
    public bool Reload() {
        if(state == State.Reloading || ammoRemain <= 0 || magAmmo >= magCapacity){
            return false;
        }

        StartCoroutine(ReloadRoutine());
        return true;
    }

    // 실제 재장전 처리를 진행
    private IEnumerator ReloadRoutine() {
        // 현재 상태를 재장전 중 상태로 전환
        state = State.Reloading;

        gunAudioPlayer.PlayOneShot(reloadClip);
        
        // 재장전 소요 시간 만큼 처리를 쉬기
        yield return new WaitForSeconds(reloadTime);

        int ammoToFill = magCapacity - magAmmo;

        if(ammoRemain < ammoToFill)
            ammoToFill = ammoRemain;

        magAmmo += ammoToFill;
        ammoRemain -= ammoToFill;

        // 총의 현재 상태를 발사 준비된 상태로 변경
        state = State.Ready;
    }
}