namespace WeaponMaster.Movement
{
    /// <summary>
    /// 캐릭터 표현에서 조회하는 실제 이동 속도다.
    /// </summary>
    public interface IMovementSpeed
    {
        float CurrentSpeed { get; }
    }
}
