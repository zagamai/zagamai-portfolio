using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System; //아래 리스트 인벤토리아이템을 넣으려면 유징을 만들어야됨


// github연동 확인용 스크립터

public class NewMonoBehaviourScript : MonoBehaviour
//인벤토리 ui만들기
{
    PanelRenderer renderer;//인스펙터에서 볼 수 있는 애
    VisualElement slotContainer; //ui툴킷을 열었을 때 보이는 애들
    Label itemInfoLabel; //ui툴킷을 열었을 때 보이는 애들
    //인벤토리 구조화
    public int totalSlotCount = 12; //인벤토리 총 칸수
    public List<InventoryItem> initItems = new List<InventoryItem>(); //초기 아이템
    //리스트에서 자료의 수를 세는 방법 List.Count
    InventoryItem[] slots; //아이템 칸
    bool isInit = false; //초기화가 된 인벤토리인지를 확인, 씬변환이나 세이브 로드시에 보존할 것.
    //슬롯(인벤토리) 초기화 메서드. 인벤토리 전체의 컨트롤러임.
    VisualElement root;  //드랍포인트 보정용
    VisualElement dragGhostIcon;  //고스트 아이콘
    bool isDragging = false;  //클릭 이후 떼기 전까지 구별용
    int dragStartIndex = -1;  //클릭된 아이템의 번호저장
    int dragEndIndex = -1;  //내려놓을 아이템의 번호저장


    void InitSlotData()
    {
        if (isInit == true)
        {
            return;
        }
        slots = new InventoryItem[totalSlotCount]; //인벤토리 총 칸수 구현
        for (int i = 0; i < totalSlotCount; i++)
        {
            if (i < initItems.Count && initItems[i] != null)
            {
                //초기지급아이템을 빈 인벤토리에 저장
                slots[i] = initItems[i].Clone(); //주소참조로인한 공멸방지
            }
            else
            {
                slots[i] = new InventoryItem(); //빈칸을 저장
            }
        }
        isInit = true;
    }
    void Start()
    {
        InitSlotData();
        renderer = GetComponent<PanelRenderer>();
        renderer.RegisterUIReloadCallback(OnUIReload); //렌더러에 아래 만든 메서드를 주입시킴 ui그릴 때 실행해줌
    }

    void OnRootPointUp(PointerUpEvent e)
    {
        if (isDragging == false)     // (! isDragging) 과 동일.
        {
            return;                 // 마우스를 땐 상태에서는 아무것도 작동하지 않지만
        }
        isDragging = false;        // 마우스를 클릭한 채라면 false로 변경해 둔다.
        if (dragGhostIcon != null)
        {
            dragGhostIcon.style.display = DisplayStyle.None;
        }
        if (dragEndIndex >= 0 || dragEndIndex < totalSlotCount && dragEndIndex != dragStartIndex)
        {
            // 아이템 교체 또는 합성기능 추가부분
        }
        dragStartIndex = -1;
    }
    void HandItemDrop(int fromIndex, int tolndex)
    {
        InventoryItem fromItem = slots[fromIndex];
        InventoryItem toItem = slots[tolndex];
        if (toItem.IsEmpty() == true)               //목적지의 아이템이 빈칸인 경우.
        {
            slots[tolndex] = fromItem.Clone();      // 복사
            slots[fromIndex].Clear();               // 원래것 지움
            itemInfoLabel.text = "[" + slots[tolndex].name + "] 아이템을 " + (tolndex + 1) + "번 슬롯으로 이동 !!";
        }
        else if (fromItem.IsSameItem(toItem) == true && toItem.maxStack >1)
        {
            // 이름이 똑같고 소모품인 것을 드래그해서 합친 상황.
            int totalAmount = toItem.amount + fromItem.amount;
            if(totalAmount <= toItem.maxStack)
            {
                toItem.amount = totalAmount;
                fromItem.Clear();
                itemInfoLabel.text = "[" + slots[tolndex].name + "]수량을 합쳤습니다 !(현재수량 : " + totalAmount + ")";
            }
            else
            {
                int remain = totalAmount - toItem.maxStack;
                fromItem.amount = remain;
                toItem.amount = toItem.maxStack;
                itemInfoLabel.text = "[" + slots[tolndex].name + "] 최대수량 !!!(" + toItem.amount + "개)까지 합쳤습니다.";
            }
        }
        else
        {
            InventoryItem temp = slots[tolndex];
            slots[tolndex]= slots[fromIndex];
            slots[fromIndex] = temp;
            itemInfoLabel.text = "[" + slots[tolndex].name + "] 과 ]"+ slots[tolndex].name +"] 아이템 위치를 교환했습니다.";

        }
        BuildInventorySlots();
    }


    //Panel Renderer로 부터 내부 Element를 추출하기 위해 반드시 갖춰야 하는 규격
    //void타입의 메서드여야 하고 매개변수로 PanelRenderer, VisualElement, int 세개의 값을 가져야 함.
    //이름은 맘대로 지어도 된다.
    //1번 패러미터 PanelRenderer renderer : 함수를 실행시킨 Renderer가 해당 매개변수에 저장
    //2번 패러미터 VisualElement root : 함수를 실행시킨 Renderer가 가진 UXML파일의 root를 해당 매개변수에 저장
    //3번 패러미터 int version : Renderer가 수회 다른 UXML을 변경해가며 그려야 할 경우 비교의 대상이 될 고유번호 지정
    void OnUIReload(PanelRenderer renderer, VisualElement root, int version)
    {//스크립트에서 UXML파일을 바꾸기 (UI그림을 여기서 그림)
        this.root = root;
        slotContainer = root.Q<VisualElement>("SlotContainer");
        itemInfoLabel = root.Q<Label>("ItemInfoLabel");
        CreateDragGhostIcon();
        root.RegisterCallback<PointerMoveEvent>(OnRootPointerMove);
        root.RegisterCallback<PointerUpEvent>(OnRootPointUp);
        BuildInventorySlots();
    }
    //유령 아이콘 제작
    //CSS에 써야되는거임 여기다가 쓰면 욕먹음
    void CreateDragGhostIcon()
    {
        dragGhostIcon = new VisualElement();
        dragGhostIcon.name = "Ghost";

        dragGhostIcon.style.position = Position.Absolute;
        dragGhostIcon.style.width = 64;
        dragGhostIcon.style.height = 64;
        dragGhostIcon.style.opacity = 0.5f;
        //dragGhostIcon.style.backgroundColor = Color.red;//고스트 커서가 눈에 보임 레드색으로

        dragGhostIcon.pickingMode = PickingMode.Ignore;
        dragGhostIcon.style.display = DisplayStyle.None;//고스트 커서가 눈에안보이게함
        root.Add(dragGhostIcon);

    }
    void OnRootPointerMove(PointerMoveEvent e)
    {
        if(isDragging == false || dragGhostIcon == null)
        {
            return;
        }
        dragGhostIcon.style.left = e.position.x - 32;
        dragGhostIcon.style.top = e.position.y - 32;
    }
    void BuildInventorySlots()
    {
        if (slotContainer == null)
        {
            return;
        }
        //클리어가 그 부속물의 연결점을 끊는 코드임
        //슬롯컨테이너와 연결되어있는 무언가와 연결점을 끊는거임
        slotContainer.Clear(); //VisualElement.Clear() 자식 Node를 제거하는 기능. 원래 있는 기능
                                //InventoryItem.Clear() 객체 내부의 데이터를 0으로 변경. 우리가 만든 기능
        for (int i = 0; i < totalSlotCount; i++) //인벤토리 갯수의 12번 반복 시키는 포문
        {
            VisualElement slotElement= new VisualElement(); //비주얼엘리멘트를 생성
            slotElement.name = "Slot_" + i; //슬롯의 고유 번호를 새겨주는 코드(슬롯컨테이너처럼)ex) #슬롯1, #슬롯2....
            slotElement.AddToClassList("inventory-slot");
            slotElement.userData = i; //유저데이터타입이 인티저 타입으로 생성
            slotElement.RegisterCallback<PointerDownEvent>(OnSlotPointerDown);
            slotElement.RegisterCallback<PointerUpEvent>(OnSlotPointerUp);
            slotElement.RegisterCallback<PointerEnterEvent>(OnSlotPointerEnter);
            slotElement.RegisterCallback<PointerLeaveEvent>(OnSlotPointerLeave);
            InventoryItem currentItem = slots[i]; //'슬롯즈'는 인벤토리안에 들어갈 작은 아이템 박스의 갯수임/현재의아이템
            //인벤토리아이템은 밑에 스트링2개 인티저2개 뭐 이런식으로 생성한 타입임
            if (currentItem.IsEmpty() == false) //밑에 빈칸인지 확인해주는 코드가 있으니 현재아이템안에 이즈엠티가 거짓이면
            {
                //아이콘 표시 처리
                VisualElement iconElement = new VisualElement();//아이콘엘리멘트 생성.
                iconElement.AddToClassList("slot-icon");
                iconElement.pickingMode = PickingMode.Ignore; //마우스 가로채기 금지(슬롯이 클릭이 되게끔해주는거)
                if (currentItem.icon != null)
                {
                    iconElement.style.backgroundImage = new StyleBackground(currentItem.icon);
                }
                slotElement.Add(iconElement);//애드하지않으면 동작하지않는다 라는 뜻
                //수량 표시 처리
                Label currentLabel = new Label(""+currentItem.amount);//스트링타입에 인티저타입을 쓰려고 하니 오류가 남 그럴때는 ""+ 이렇게 하면됨
                currentLabel.AddToClassList("slot-label");
                currentLabel.pickingMode = PickingMode.Ignore; //마우스 가로채기 금지
                slotElement.Add(currentLabel);
            }
            slotContainer.Add(slotElement);
        }
    }//UI그림그리는 메서드 클래스 종료지점
    //마우스 포인터가 특정 슬롯에 들어간 경우 이벤트
    void OnSlotPointerEnter(PointerEnterEvent e)
    {
        VisualElement slot = (VisualElement)e.currentTarget;
        if(slot == null && slot.userData == null)
        {
            dragEndIndex = (int)slot.userData;
        }
    }
    void OnSlotPointerLeave(PointerLeaveEvent e)
    {
        VisualElement slot = (VisualElement)e.currentTarget;
        if (slot == null && slot.userData == null)
        {
            int index = (int)slot.userData;
            if(dragEndIndex == index)
            {
                dragEndIndex = -1;
            }
        }
    }
    void OnSlotPointerUp(PointerUpEvent e)
    {
        if(isDragging == false)
        {
            return;
        }
        isDragging = false;
        if(dragGhostIcon != null)
        {
            dragGhostIcon.style.display = DisplayStyle.None;
        }
        //아이템교체, 아이템 합침, 아이템 조합, 아이템 이동
        //개별적으로 조건 처리 필요함
        if (dragEndIndex >=0 && dragEndIndex < totalSlotCount &&dragEndIndex != dragStartIndex)
        {
            HandItemDrop(dragStartIndex, dragEndIndex);
        }
        dragStartIndex = -1;
    }
    void OnSlotPointerDown(PointerDownEvent e)//마우스 포인트 인벤토리 클릭 이벤트
    {
        VisualElement clickedSlot = (VisualElement)e.currentTarget; //비주얼엘리멘트 클릭드슬롯은
        if (clickedSlot == null || clickedSlot.userData == null) //클릭슬롯이 널이거나 클릭슬롯 유저데이터가 널일경우
            //플레이하는 입장에서 눈으로 보고 느낄 수 있는 것이 유저데이터임
        {
            return;//동작하지마
        }
        int slotIndex = (int)clickedSlot.userData;//클릭드슬롯 유저데이터를 인티저타입으로 강제로 뽑아옴
        if (e.button == 0)
        {
            //왼쪽 마우스 눌림
            ShowItemInfo(slotIndex);
            //드래그 시작, 고스트 등장
            if (slots[slotIndex].IsEmpty() == false)//빈칸은 클릭해도 고스트가 등장하지 못하게 하는 코드
            {
                isDragging = true;
                dragStartIndex = slotIndex;
                dragGhostIcon.style.backgroundImage = new StyleBackground(slots[slotIndex].icon);
                dragGhostIcon.style.display = DisplayStyle.Flex;
                dragGhostIcon.style.left = e.position.x - 32;
                dragGhostIcon.style.top = e.position.y - 32;
            }
        }
        else if(e.button == 1)
        {
            //마우스 오른쪽 눌림, 아이템 사용기능
            UseItem(slotIndex);
            BuildInventorySlots();
        }
    }
    void ShowItemInfo(int index)
    {
        InventoryItem clickedItem = slots[index];
        if (clickedItem.IsEmpty() ==true)
        {
            itemInfoLabel.text = (index + 1) + "번 슬롯은 비어있습니다.";
        }
        else
        {
            itemInfoLabel.text = "[" + clickedItem.name + "]" + clickedItem.description;
        }
    }
    void UseItem(int index)
    {
        InventoryItem item = slots[index];
        if(item.IsEmpty() == true)
        {
            itemInfoLabel.text = (index + 1) + "번 슬롯에는 사용할 아이템이 없습니다.";
            return;
        }
        if(item.maxStack <= 1)
        {
            itemInfoLabel.text = "["+item.name+"] 아이템은 소모품이 아닙니다.";
            return;
        }
        string usedItem = item.name;
        item.amount -= 1;
        if(item.amount <= 0)
        {
            item.Clear(); //아이템 소멸
            itemInfoLabel.text = "[" + usedItem + "] 모두 소모하였습니다.";
        }
        else
        {
            itemInfoLabel.text = "[" + usedItem + "] 아이템 사용! 남은 수량("+ item.amount + "개)";
        }
    }
}

[Serializable]//컴퓨터만 읽을 수 있는 코드
public class InventoryItem
{
    //패러미터는 10개 내외로 하자. 난 찌끄래기 초보니까
    //public string id; //아이템 고유번호 넣어줄 수 있음.
    public string name; //물약
    public string description; //다쳤을 때 사용 HP50회복
    public Sprite icon; //물약 모양
    public int amount; //수량
    public int maxStack; //최고치 수량(인벤에서 겹칠 수 있는)

    public InventoryItem() //생성자라고 하는 것
    {
        Clear();

    }
    //생서는 객체 생성시 '무조건' 1회만 실행
    public InventoryItem(string a, string b, Sprite c, int d, int e) //생성자라고 하는 것
    {
        name = a;
        description = b;
        icon = c;
        amount = d;
        maxStack = e;
    }
   public void Clear()//공허의 비슷한 데이터가 있어야 인벤토리에서 물건을 옮길 수 있음
    {
        name = "";
        description = "";
        icon = null;
        amount = 0;
        maxStack = 0;
    }
    public InventoryItem Clone()//복제할 코드 생성 - 별도의 클론 메서드를 작성해서 오류를 방지한다. 
    {
        return new InventoryItem(name, description, icon, amount, maxStack);
    }
    public bool IsEmpty() //지금 클릭된 좌표의 데이터가 빈칸인지 아닌지 판별
    {
        if(name == null || name == "" || amount == 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    public bool IsSameItem(InventoryItem other)
    {
        if(other == null)
        {
            return false;
        }if(this.IsEmpty() == true || other.IsEmpty() == true)
        {
            return false;
        }
        if(this.name == other.name)
        {
            return true;
        }
        else
        {
            return false; //다른 경우의 예외가 발생할 때 변경할 것.
        }
    }
}