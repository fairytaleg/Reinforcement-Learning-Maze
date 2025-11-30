using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class MazeAgent : Agent
{
    [Header("Referanslar")]
    public MazeGenerator mazeGenerator;
    public Transform targetTransform;
    public Transform environmentParent; // Labirentin ana objesi

    [Header("Ayarlar")]
    public float moveSpeed = 5f;

    private Rigidbody rb;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();

        // --- KRİTİK DÜZELTME ---
        // Ajanın boyutunu kodla otomatik olarak küçültüyoruz (0.6 birim).
        // Böylece 1 birimlik koridorlarda doğar doğmaz duvarlara sürtüp "ölmez".
        transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
    }

    public override void OnEpisodeBegin()
    {
        // 1. Labirenti yeniden oluştur
        mazeGenerator.GenerateMaze(environmentParent);

        // 2. Ajanı başlangıç noktasına koy
        // Yükseklik (y) 0.5f, zemin (0) üzerine tam oturması için idealdir.
        transform.localPosition = new Vector3(0, 0.5f, 0);
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // 3. Hedefi en uzak köşeye yerleştir (width-1, height-1)
        targetTransform.localPosition = new Vector3(mazeGenerator.width - 1, 0.5f, mazeGenerator.height - 1);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Toplam Gözlem Sayısı (Space Size): 9

        // Ajanın kendi pozisyonu (3 değer: x, y, z)
        sensor.AddObservation(transform.localPosition);

        // Hedefin pozisyonu (3 değer: x, y, z)
        sensor.AddObservation(targetTransform.localPosition);

        // Ajan ile hedef arasındaki fark vektörü (3 değer: x, y, z)
        sensor.AddObservation(targetTransform.localPosition - transform.localPosition);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Hareket Kararları: 0=Dur, 1=İleri, 2=Geri, 3=Sağ, 4=Sol
        int moveAction = actions.DiscreteActions[0];

        Vector3 dirToGo = Vector3.zero;

        if (moveAction == 1) dirToGo = Vector3.forward;
        if (moveAction == 2) dirToGo = Vector3.back;
        if (moveAction == 3) dirToGo = Vector3.right;
        if (moveAction == 4) dirToGo = Vector3.left;

        // Hareketi uygula
        transform.Translate(dirToGo * moveSpeed * Time.deltaTime);

        // Varolma Cezası: 
        // Her adımda çok küçük bir ceza ver ki oyalanmadan hedefe gitsin.
        AddReward(-0.001f);
    }

    // Çarpışma Mantığı
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            // Duvara çarpınca ceza ver ve bölümü bitir (Yeniden başlat)
            // Eğer sürekli buraya giriyorsa, ajan hala çok büyük veya zemin "Wall" etiketli olabilir.
            AddReward(-1.0f);
            EndEpisode();
        }
        else if (collision.gameObject.CompareTag("Target"))
        {
            // Hedefe ulaşınca büyük ödül ver ve bölümü bitir
            Debug.Log("Hedefe Ulaştı!");
            AddReward(10.0f);
            EndEpisode();
        }
        // ÖNEMLİ: Zemin (Floor) etiketli objeye çarpınca hiçbir şey olmaz.
        // Eğer zemin "Wall" etiketliyse ajan hareket edemez, hemen ölür.
    }

    // Klavye ile Test İçin (Heuristic Mod)
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        discreteActionsOut[0] = 0;

        if (Input.GetKey(KeyCode.W)) discreteActionsOut[0] = 1;
        if (Input.GetKey(KeyCode.S)) discreteActionsOut[0] = 2;
        if (Input.GetKey(KeyCode.D)) discreteActionsOut[0] = 3;
        if (Input.GetKey(KeyCode.A)) discreteActionsOut[0] = 4;
    }
}