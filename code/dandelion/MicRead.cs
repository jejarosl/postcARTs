using B83.MathHelpers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MicRead : MonoBehaviour
{
    public GameObject sp;
    private AudioClip c;
    public int deviceNum = 0;
    public float threshold = 0.95f;

    List<GameObject> spectrogram = new List<GameObject>();
    int maxSamples = 1024;

    float[] data = new float[0];
    FFT fft = new FFT();

    const int segments = 15;
    float[] segmentsMean = new float[segments];
    float[] segmentsStd = new float[segments];
    float[] segmentsN = new float[segments];
    public bool isBlowing = false;
    void Start()
    {
        Debug.Log("Device amount:" + Microphone.devices.Length);

        c = Microphone.Start(Microphone.devices[deviceNum], true, 1, maxSamples);//24000
        StartCoroutine(RecordLoop());
        for (int i = 0; i < maxSamples/2; i++)
        {
            var go = GameObject.Instantiate(sp, sp.transform.parent);
            go.SetActive(true);
            go.transform.position = sp.transform.position + i * Vector3.right * 0.01f;

            spectrogram.Add(go);
        }
    }

    float Semblance(float[] num)
    {
        float sum = 0;
        float sumSq = 0;
        for (int i = 0; i < num.Length; i++)
        {
            sum += num[i];
            sumSq += num[i] * num[i];
        }
        return sum * sum / ((float)num.Length * sumSq);
    }

    private IEnumerator RecordLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.15f);


            int currentPosition = Microphone.GetPosition(Microphone.devices[deviceNum]);

            if (data.Length < c.samples * c.channels)
                data = new float[c.samples * c.channels];
            c.GetData(data, 0);

            Complex[] fftRes = FFT.CalculateFFT(FFT.Float2Complex(data), false);

            for (int i = 0; i < fftRes.Length/2; i++)
            {
                int spectrogramBin = Mathf.RoundToInt((float)(maxSamples - 1) * (float)i / (float)fftRes.Length);
                Vector3 pos = spectrogram[spectrogramBin].transform.position;
                pos.y = Mathf.Abs(fftRes[i].fMagnitude) * 100.0f;
                float value = Mathf.Abs(fftRes[i].fMagnitude) * 100.0f;
                float r = (value > 0.5) ? (value - 0.5f) * 2.0f:0;
                float g = (Mathf.Abs(value-0.5f) < 0.25 ) ? (0.25f - Mathf.Abs(value - 0.5f) )/ 0.25f : 0;
                float b = (value < 0.5) ? (0.5f - value) / 0.5f : 0;

                spectrogram[spectrogramBin].GetComponent<MeshRenderer>().material.color = new Color(r,g,b);
                spectrogram[spectrogramBin].transform.position = pos;
            }

            for (int s = 0; s < segments; s++)
            {
                segmentsMean[s] = 0;
                segmentsStd[s] = 0;
                segmentsN[s] = 0;
            }
            for (int i = 0; i < fftRes.Length; i++)
            {
                int segment = Mathf.RoundToInt((float)(segments - 1) * 2.0f * (float)i / (float)fftRes.Length);
                if (segment < segments)
                {
                    segmentsMean[segment] += Mathf.Abs(fftRes[i].fMagnitude);
                    segmentsStd[segment] += Mathf.Abs(fftRes[i].fMagnitude) * Mathf.Abs(fftRes[i].fMagnitude);
                    segmentsN[segment] += 1.0f;
                }
            }
            for (int s = 0; s < segments; s++)
            {
                if (segmentsN[s] != 0)
                {
                    segmentsMean[s] = segmentsMean[s] / segmentsN[s];
                    segmentsStd[s] = Mathf.Sqrt(segmentsStd[s] / segmentsN[s] - segmentsMean[s] * segmentsMean[s]);
                }
                else
                {
                    Debug.Log("Zero point in segment");
                }
            }
            float s1 = Semblance(segmentsMean);
            float s2 = Semblance(segmentsStd);
            Debug.Log("S1:" + s1.ToString() + "  S2:" + s2.ToString());
            if (s1 >= threshold && s2 >= threshold)
                isBlowing = true;
            else
                isBlowing = false;
        }
    }
}
