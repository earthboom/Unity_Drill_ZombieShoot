using Photon.Pun;
using UnityEngine;
using UnityEngine.UI; // UI 관련 코드

// 플레이어 캐릭터의 생명체로서의 동작을 담당
public class PlayerHealth : LivingEntity {
    public Slider healthSlider; // 체력을 표시할 UI 슬라이더

    public AudioClip deathClip; // 사망 소리
    public AudioClip hitClip; // 피격 소리
    public AudioClip itemPickupClip; // 아이템 습득 소리

    private AudioSource playerAudioPlayer; // 플레이어 소리 재생기
    private Animator playerAnimator; // 플레이어의 애니메이터

    private PlayerMovement playerMovement; // 플레이어 움직임 컴포넌트
    private PlayerShooter playerShooter; // 플레이어 슈터 컴포넌트

    private void Awake() {
        // 사용할 컴포넌트를 가져오기
        playerAnimator = GetComponent<Animator>();
        playerAudioPlayer = GetComponent<AudioSource>();

        playerMovement = GetComponent<PlayerMovement>();
        playerShooter = GetComponent<PlayerShooter>();
    }

    protected override void OnEnable() {
        // LivingEntity의 OnEnable() 실행 (상태 초기화)
        base.OnEnable();

        healthSlider.gameObject.SetActive(true);
        healthSlider.maxValue = startingHealth;
        healthSlider.value = health;

        playerMovement.enabled = true;
        playerShooter.enabled = true;
    }

    // 체력 회복
    [PunRPC]
    public override void RestoreHealth(float newHealth) {
        // LivingEntity의 RestoreHealth() 실행 (체력 증가)
        base.RestoreHealth(newHealth);
        healthSlider.value = health;
    }

    // 데미지 처리
    [PunRPC]
    public override void OnDamage(float damage, Vector3 hitPoint, Vector3 hitDirection) {
        // LivingEntity의 OnDamage() 실행(데미지 적용)
        if(!dead){
            playerAudioPlayer.PlayOneShot(hitClip);
        }

        base.OnDamage(damage, hitPoint, hitDirection);
        healthSlider.value = health;
    }

    // 사망 처리
    public override void Die() {
        // LivingEntity의 Die() 실행(사망 적용)
        base.Die();

        healthSlider.gameObject.SetActive(false);

        playerAudioPlayer.PlayOneShot(deathClip);
        playerAnimator.SetTrigger("Die");

        playerMovement.enabled = false;
        playerShooter.enabled = false;

        Invoke("Respawn", 5.0f);
    }

    private void OnTriggerEnter(Collider other) {
        // 아이템과 충돌한 경우 해당 아이템을 사용하는 처리
        if(!dead){
            IItem item = other.GetComponent<IItem>();
            if(item != null){
                // 호스트만 아이템 직접 사용 가능.
                // 호스트에서는 아잍메 사용 후 사욍돈 아이템의 효과를 모든 클라이언트에 동기화.
                if(PhotonNetwork.IsMasterClient){
                    item.Use(gameObject);
                }
                
                playerAudioPlayer.PlayOneShot(itemPickupClip);
            }
        }
    }

    // 부활 처리
    public void Respawn(){
        // 로컬 플레이어만 직접 위치 변경 가능
        if(photonView.IsMine){
            Vector3 randomSpawnPos = Random.insideUnitSphere * 5.0f;
            randomSpawnPos.y = 0.0f;

            transform.position = randomSpawnPos;
        }

        gameObject.SetActive(false);
        gameObject.SetActive(true);
    }
}