public interface IStatableItem
{
    /// <summary>
    /// 아이템 인스턴스(개별 데이터) 번호
    /// </summary>
    int ItemInstanceNum { get; set; }

    /// <summary>
    /// 현재 아이템 인스턴스의 상태 데이터 가져오기
    /// </summary>
    /// <returns>현재 아이템 인스턴스 상태 데이터</returns>
    IItemStateData GetStateData();

    /// <summary>
    /// 현재 아이템 인스턴스의 상태 데이터를 해당 데이터로 설정(갱신)하기
    /// </summary>
    /// <param name="data">상태 데이터</param>
    void SetStateData(IItemStateData data);
}