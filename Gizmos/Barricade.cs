using System.Collections;
using UnityEngine;

public class Barricade : MonoBehaviour
{
    private Animator m_anim;
    public Transform nodePostion;

    public Board[] boards = new Board[5];

    public AudioSource m_AudioMain;
    public AudioSource m_AudioRepairSting;
    public AudioClip repairSting;

    public AudioClip[] builtSounds;
    public AudioClip[] destroySounds;

    //open is true closed is false
    public bool hasBoardsRemoved;
    public bool allBoardsRemoved;
    private float repairDelay;

    public Board board1;
    public Board board2;
    public Board board3;
    public Board board4;
    public Board board5;
    void Start()
    {
        m_anim = GetComponent<Animator>();
        //assign boards
        boards[0] = new Board(1);
        boards[1] = new Board(2);
        boards[2] = new Board(3);
        boards[3] = new Board(4);
        boards[4] = new Board(5);

        //debug
        board1 = boards[0];
        board2 = boards[1];
        board3 = boards[2];
        board4 = boards[3];
        board5 = boards[4];
    }

    void Update()
    {
        CheckBoards();
    }

    private void CheckBoards()
    {
        //intial check
        foreach(Board board in boards)
        {
            if (board.isRemoved)
            {
                hasBoardsRemoved = true;
                break;
            }
            else
            {
                hasBoardsRemoved = false;
            }
        }

        if (board1.isRemoved && board2.isRemoved && board3.isRemoved && board4.isRemoved && board5.isRemoved)
            allBoardsRemoved = true;
        else
            allBoardsRemoved = false;
    }
    public void RemoveBoard(int boardIndex)
    {
        //layers are needed as board removal order is random
        //set multiplier to play anim forward
        boards[boardIndex - 1].isRemoved = true;
        m_anim.Play(string.Format("barr_{0}", boardIndex), boardIndex);
        StartCoroutine(PlaySound(destroySounds[boardIndex - 1]));

    }
    public void RepairBoard()
    {
        m_AudioRepairSting.PlayOneShot(repairSting);
        //board index reverse animation and retoggle boolean
        int boardIndex = 0;
        foreach (Board board in boards)
        {
            if (board.isRemoved)
            {
                boardIndex = board.index;
                //we only want one board to be selected
                break;
            }
        }

        //set board as rebuilt again
        boards[boardIndex - 1].isRemoved = false;
        //set play anim backwards
        m_anim.Play(string.Format("barr_{0}_rebuild", boardIndex), boardIndex);
        StartCoroutine(PlaySound(builtSounds[boardIndex - 1]));
    }

    IEnumerator PlaySound(AudioClip audToPlay)
    {
        yield return new WaitForSeconds(0.6f);
        m_AudioMain.PlayOneShot(audToPlay);
    }
    
    void OnTriggerStay(Collider col)
    {
        if (col.tag == "Player" && hasBoardsRemoved)
        {
            UI ui = col.GetComponent<UI>();
            ui.PromptText.text = "Press F to repair barrier";

            if (Input.GetButton("Use") && repairDelay <= Time.time)
            {
                Player player = col.GetComponent<Player>();
                repairDelay = Time.time + 1;
                RepairBoard();
                player.AddScore(10);
            }
        }
    }
    void OnTriggerExit(Collider col)
    {
        if (col.tag == "Player")
        {
            col.GetComponent<UI>().PromptText.text = "";
        }
    }


    [System.Serializable]
    public class Board
    {
        public int index;
        public bool isRemoved;

        public Board(int index)
        {
            this.index = index;
        }
    }

}