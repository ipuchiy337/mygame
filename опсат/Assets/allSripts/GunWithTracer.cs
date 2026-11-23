using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GunWithTracer : MonoBehaviour
{
    [Header("References")]
    public Transform muzzlePoint;
    public ParticleSystem muzzleFlash;
    public GameObject bulletPrefab;
    public Player player;

    [Header("Gun Rotation")]
    public Transform gunPivot;
    public float rotateSpeed = 5f;
    public float aimThreshold = 10f;        // ФИКС: было 5f — слишком строго, Slerp не успевал
    [Header("Гильзы")]
    public GameObject shellPrefab;        // prefab гильзы
    public Transform shellEjectPoint;     // точка выброса (сбоку от затвора)
    public float shellForce = 3f;         // сила выброса
    public float shellTorque = 5f;        // вращение гильзы
    [Header("Settings")]
    public float range = 100f;
    public float fireRate = 0.2f;
    public float damage = 25f;
    public float bulletSpeed = 120f;
    public float bullets;
    public float Magaz;

    [Header("Auto Aim")]
    public float autoShootDistance = 15f;
    public LayerMask enemyMask;

    [Header("Accuracy / Spread")]
    public float spreadAngle = 5f;

    [Header("Ricochet")]
    public int currentRicochets = 0;
    public float currentRicochetChance = 0f;
    public float ricochetSearchRadius = 15f;

    [Header("Passives")]
    public float addFirerate;
    public float addDamage;
    public float addMagaz;
    public float addTochnost;
    public float addRange;
    public float add2x;

    public float maxFireRate;
    public float maxDamaged;
    public float maxMagaz;
    public float maxTochnost;
    public float maxRange;
    public float max2x;

    [Header("UI")]
    public Text Bullets;
    public Text _admg, _afir, _amag, _atohnost, _arange, _a2x;
    public GameObject UImag, UIfireRate, UIdmg, UItochnost, UIrange, UI2x;
    public GameObject amoInf;
    public GameObject activka2x;

    [Header("Info")]
    public int GunID;
    public string GunName;
    public Sprite Icon;
    public GameObject dropPrefab;

    public AudioSource shootSound;
    public AudioClip shootClip;

    private float nextFireTime;
    private Transform autoTarget;
    private float timerOff;
    private bool isAimedAtTarget = false;

    // ФИКС: флаг чтобы Invoke("reload") не спамил каждый кадр
    private bool isReloading = false;

    void Update()
    {
        if (Bullets != null)
            Bullets.text = bullets.ToString();

        // ФИКС: проверяем isReloading чтобы не вызывать Invoke каждый кадр
        if (bullets <= 0 && !isReloading)
        {
            isReloading = true;
            Invoke("reload", 2f);
        }

        FindAutoTarget();
        RotateGunToTarget();
        TryShoot();
        UpdateUI();
    }

    void FindAutoTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, autoShootDistance, enemyMask);
        float minDist = Mathf.Infinity;
        autoTarget = null;

        foreach (var hit in hits)
        {
            float dist = Vector3.Distance(transform.position, hit.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                autoTarget = hit.transform;
            }
        }

        // ФИКС: timerOff считается только когда цели нет
        if (autoTarget != null)
        {
            amoInf.SetActive(true);
            timerOff = 0f;
        }
        else
        {
            timerOff += Time.deltaTime;
            if (timerOff >= 5f)
                amoInf.SetActive(false);
        }
    }

    void RotateGunToTarget()
    {
        if (gunPivot == null || autoTarget == null)
        {
            isAimedAtTarget = false;
            return;
        }

        Vector3 dirToTarget = autoTarget.position - gunPivot.position;

        if (dirToTarget.sqrMagnitude < 0.001f)
        {
            isAimedAtTarget = false;
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(dirToTarget);

        gunPivot.rotation = Quaternion.Slerp(
            gunPivot.rotation,
            targetRotation,
            Time.deltaTime * rotateSpeed
        );

        float angleToTarget = Quaternion.Angle(gunPivot.rotation, targetRotation);
        isAimedAtTarget = angleToTarget <= aimThreshold;
    }

    void TryShoot()
    {
        if (!isAimedAtTarget) return;
        if (autoTarget == null) return;
        if (Time.time < nextFireTime) return;
        if (bullets < 1) return;

        // ФИКС: проверяем что цель не за препятствием
        Vector3 aimPoint = autoTarget.position + Vector3.up * 1f;
        Vector3 dirToTarget = (aimPoint - muzzlePoint.position).normalized;
        float distToTarget = Vector3.Distance(muzzlePoint.position, aimPoint);

        if (Physics.Raycast(muzzlePoint.position, dirToTarget, out RaycastHit hit, distToTarget))
        {
            // Если луч попал не в зомби — значит что-то мешает, не стреляем
            if (!hit.collider.CompareTag("Zombie")) return;
        }

        nextFireTime = Time.time + fireRate;
        Shoot();
    }

    void Shoot()
    {
        if (muzzleFlash != null)
            muzzleFlash.Play();

      

        bullets--;
        if (shellPrefab != null && shellEjectPoint != null)
        {
            GameObject shell = Instantiate(shellPrefab, shellEjectPoint.position, shellEjectPoint.rotation);
            Rigidbody shellRb = shell.GetComponent<Rigidbody>();
            if (shellRb != null)
            {
                // Выбрасываем вправо и немного вверх от точки выброса
                Vector3 ejectDir = shellEjectPoint.right + shellEjectPoint.up * 0.5f;
                shellRb.linearVelocity = ejectDir * shellForce;
                // Случайное вращение для реализма
                shellRb.angularVelocity = new Vector3(
                    Random.Range(-shellTorque, shellTorque),
                    Random.Range(-shellTorque, shellTorque),
                    Random.Range(-shellTorque, shellTorque)
                );
            }
            // Уничтожаем гильзу через 3 секунды
            Destroy(shell, 5f);
        }
        if (shootSound != null && shootClip != null)
            shootSound.PlayOneShot(shootClip);

        Vector3 aimPoint = autoTarget.position + Vector3.up * 0.9f; // 1f = примерно центр тела
        Vector3 shootDir = (aimPoint - muzzlePoint.position).normalized;
        // Разброс растёт с дистанцией: вблизи почти точно, далеко — сильный разброс
        float dist = Vector3.Distance(muzzlePoint.position, autoTarget.position);
        float dynamicSpread = spreadAngle * (dist / autoShootDistance);
        shootDir = ApplySpread(shootDir, dynamicSpread);

        GameObject bullet = Instantiate(bulletPrefab, muzzlePoint.position, Quaternion.LookRotation(shootDir));

        // ФИКС: игнорируем коллизию пули с самим игроком чтобы пуля не билась о него
        if (player != null)
        {
            Collider bulletCol = bullet.GetComponent<Collider>();
            Collider playerCol = player.GetComponent<Collider>();
            if (bulletCol != null && playerCol != null)
                Physics.IgnoreCollision(bulletCol, playerCol);
        }

        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
            rb.linearVelocity = shootDir * bulletSpeed;

        Bullet bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript != null)
        {
            bulletScript.damage = damage;
            bulletScript.SetRicochetStats(
                currentRicochets,
                currentRicochetChance,
                enemyMask,
                ricochetSearchRadius,
                bulletSpeed
            );
        }

        // ФИКС: трассер убран отсюда — он создаётся внутри Bullet.cs чтобы не было дублирования
    }

    Vector3 ApplySpread(Vector3 direction, float angle)
    {
        if (angle <= 0f) return direction;
        float spreadX = Random.Range(-angle, angle);
        float spreadY = Random.Range(-angle, angle);
        return Quaternion.Euler(spreadY, spreadX, 0) * direction;
    }

    public void reload()
    {
        if (bullets == Magaz)
        {
            isReloading = false;
            return;
        }
        bullets = Magaz;
        isReloading = false;  // ФИКС: сбрасываем флаг после перезарядки
    }

    void UpdateUI()
    {
        if (_admg) _admg.text = maxDamaged > 1 ? maxDamaged.ToString() : "";
        if (_afir) _afir.text = maxFireRate > 1 ? maxFireRate.ToString() : "";
        if (_amag) _amag.text = maxMagaz > 1 ? maxMagaz.ToString() : "";
        if (_atohnost) _atohnost.text = maxTochnost > 1 ? maxTochnost.ToString() : "";
        if (_arange) _arange.text = maxRange > 1 ? maxRange.ToString() : "";
        if (_a2x) _a2x.text = max2x > 1 ? max2x.ToString() : "";

        if (UI2x) UI2x.SetActive(max2x > 0);
        if (UIdmg) UIdmg.SetActive(maxDamaged >= 1);
        if (UIfireRate) UIfireRate.SetActive(maxFireRate >= 1);
        if (UImag) UImag.SetActive(maxMagaz >= 1);
        if (UIrange) UIrange.SetActive(maxRange >= 1);
        if (UItochnost) UItochnost.SetActive(maxTochnost >= 1);
    }

    // Пассивки
    public void AddDamagePassive() { damage += addDamage; UIdmg.SetActive(true); maxDamaged++; }
    public void AddFireRatePassive() { fireRate -= addFirerate; UIfireRate.SetActive(true); maxFireRate++; }
    public void AddMagazPassive() { Magaz += addMagaz; bullets = Magaz; UImag.SetActive(true); maxMagaz++; }
    public void AddTochnostPassive() { spreadAngle -= 2; UItochnost.SetActive(true); maxTochnost++; }
    public void AddRangePassive() { autoShootDistance += 1.5f; UIrange.SetActive(true); maxRange++; }
    public void Add2xPassive() { max2x++; activka2x.SetActive(true); }
}