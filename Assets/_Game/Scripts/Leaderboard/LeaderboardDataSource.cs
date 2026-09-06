// using System.Collections.Generic;
// using PolyAndCode.UI;

// public class LeaderboardDataSource : IRecyclableScrollRectDataSource
// {
//     private List<LeaderboardData> _dataList = new();

//     public void UpdateData(List<LeaderboardData> data)
//     {
//         _dataList = data;
//     }

//     public int GetItemCount()
//     {
//         return _dataList != null ? _dataList.Count : 0;
//     }

//     public void SetCell(ICell cell, int index)
//     {
//         if (cell is UILeaderboardFrame frame && index >= 0 && index < _dataList.Count)
//         {
//             frame.ConfigureCell(_dataList[index]);
//         }
//     }
// }
