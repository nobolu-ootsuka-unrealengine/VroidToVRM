# VroidToVRM


・Unityアセットストアで取得したワールドを、気軽にUnityへインポートして使える 


・VRMをすぐインポートして、シェーダー適用までスムーズにできる OK

　　■ Tools > VRM - Animation Setup の使い方 
 
　　・Tools/VRM - Disable URP (All Quality Levels) すべてのレベルの Render Pipeline を None に設定してMToonを見えるようにする。
  
　　・Tools/VRM - Shader Reapplier MToonシェーダーを当てなおします プロジェクト全体
  
　　・Tools/VRM - Shader Reapplier (select Folder) MToonシェーダーを当てなおします
  


・fbxやBVHなどのモーションファイルを、VRMへ手軽にリターゲティングして再生できる OK

　　■ Tools > VRM - Animation Setup の使い方
 
　　1. Animation FBX 欄 → Project ウィンドウからアニメーション FBXまたはBVH をドラッグ
  
　　2. VRM GameObject 欄 → Hierarchy から VRM の GameObject をドラッグ
  
　　3. 「一括セットアップ実行」 を押する。


・可能なら、カメラ設定やカメラワークも手軽に操作できる

　　・新規シーンを作って

　　・VRMを0,0,0の位置に置き

　　・カメラを0,2,3の位置、0,180,0の回転で置き

　　・床を0.5,-0.5,0.5の位置、10,1,10のスケールで置き

　　・カメラ設定やカメラワークも手軽に操作できるようにして

　　カメラを全身が見えるように0,2.4,2.2の位置を 33.6,180,0の回転をデフォルトにして

　　カメラワーク（オービット）のデフォルトを

　　注目点:0,0.8,0

　　水平角Y:0

　　高さ:0

　　距離:2.35

　　にして

　　VRMを正面に向ける（リセット）で

　　カメラ設定

　　位置 : 0,0.8,2.35

　　回転:0,180,0

　　FOV:50

　　カメラワーク（オービット）を

　　注目点:0,0.8,0

　　水平角Y:0

　　高さ:0

　　距離:2.35

　　
