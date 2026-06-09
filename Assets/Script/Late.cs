using System.Linq;
using UnityEngine;

public class Late : MonoBehaviour
{
    public void Start()
    {
        {// add structs icon if they have not external
            int offset = 0;
            foreach (var type in ct.structsInfo.GetTypes())
            {
                Tick.Reg(t =>
                {
                    var info = ct.structsInfo.Get(type);
                    Tick.Cor(Projector.ProjectAsync(info.template, 256, 256, s => info.icon = s));
                }, offset++);
            }
            Tick.Reg(_ =>
            {
                ct.ctsym.IWhenIconsFinisheLoading();
                ct.ctsym.finishLoadIcon = true;
            }, offset);
        }
    }
}
