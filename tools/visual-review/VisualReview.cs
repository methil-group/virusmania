using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor;
using TMPro;
using System.IO;

public static class VisualReview
{
    static T Find<T>() where T : UnityEngine.Object => UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include).First();
    static object Field(object obj, string name) => obj.GetType().GetField(name, BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).GetValue(obj);
    static void Call(object obj, string name, params object[] args) => obj.GetType().GetMethod(name, BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).Invoke(obj,args);
    public static object Capture(string name) {
        var capture=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Unity.Pipeline.Editor.Commands.Capture.CaptureCommands")).First(t=>t!=null);
        var png=(byte[])capture.GetMethod("EncodeScreenToPng",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{Screen.width,Screen.height});
        var output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../doc/gameplay/visual-review/captures",name+".png"));
        File.WriteAllBytes(output,png);
        return new {file=name,width=Screen.width,height=Screen.height,bytes=png.Length,frame=Time.frameCount};
    }
    public static async Task<object> Windows() {
        await Run("load:Game"); await Run("accept"); await Run("stop-timer");
        string[] actions={"shop","cart","no-money","shop-close","library","scroll-middle","scroll-bottom","library-close","pause","pause-options","pause-close"};
        string[] names={"45-boutique","46-panier","47-fonds-insuffisants",null,"48-recettes-debut","49-recettes-milieu","50-recettes-cuisson",null,"51-pause","52-pause-options",null};
        for(int i=0;i<actions.Length;i++) {
            await Run(actions[i]);
            // The shop creates its product UI on first activation. Allow a rendered frame after layout.
            if(actions[i]=="shop") await Task.Delay(600);
            if(names[i]!=null) Capture(names[i]);
        }
        return new {success=true,scene=SceneManager.GetActiveScene().name,brief=Core.Brief.BriefController.Instance.actualBrief?.briefTitle};
    }
    public static async Task<object> AnalysisSamples() {
        await Resolution(1280,720); await Run("load:Game"); await Run("brief:Mutate"); await Run("accept"); await Run("stop-timer");
        var ui=Find<Core.Analysis.AnalysisInterface>();
        var briefs=Core.Brief.BriefDatabase.Instance.Database;
        string[] titles={"Mutate","Gut","Latency"};
        string[] names={"44-analyse","73-analyse-poliovirus","74-analyse-herpes"};
        for(int i=0;i<titles.Length;i++) {
            ui.ShowAnalysis(briefs.First(b=>b.briefTitle.Contains(titles[i])).wantedItem.GetHoldItem());
            await Task.Delay(900); Capture(names[i]);
        }
        ui.ShowAnalysis(briefs.First(b=>b.briefTitle.Contains("Mutate")).wantedItem.GetHoldItem());
        await Task.Delay(600); await Resolution(1920,1080); Capture("75-analyse-1080p");
        await Resolution(1024,768); Capture("76-analyse-4-3");
        await Resolution(1280,720);
        return new {success=true,samples=3};
    }
    public static async Task<object> MenuFormats() {
        await Resolution(1280,720); await Run("load:MainMenu"); Capture("01-menu");
        await Resolution(1024,768); await Task.Delay(600); Capture("72-menu-4-3");
        await Resolution(1920,1080); await Task.Delay(600); Capture("81-menu-1080p");
        await Resolution(1280,720);
        return new {success=true,formats=3};
    }
    public static async Task<object> WindowFormats() {
        await Resolution(1280,720); await Run("load:Game"); await Run("accept"); await Run("stop-timer");
        await Run("shop");
        await Resolution(1920,1080); Capture("77-boutique-1080p");
        await Resolution(1024,768); Capture("78-boutique-4-3");
        await Run("shop-close"); await Run("library");
        await Resolution(1920,1080); Capture("79-recettes-1080p");
        await Resolution(1024,768); Capture("80-recettes-4-3");
        await Resolution(1280,720);
        return new {success=true,formats=3};
    }
    public static async Task<object> TutorialScreens() {
        await Run("load:OnBoarding"); Capture("60-tutoriel-recettes");
        await Run("library"); Capture("61-tutoriel-bibliotheque"); await Run("library-close");
        string[] names={"62-tutoriel-achats","63-tutoriel-cuisson","64-tutoriel-melange","65-tutoriel-envoi"};
        for(int i=1;i<5;i++) {await Run("tutorial:"+i); Capture(names[i-1]);}
        await Run("recovery"); Capture("66-tutoriel-recuperation");
        return new {success=true,steps=5};
    }
    public static async Task<object> Moment(string action) {
        Application.runInBackground=true;
        if(action=="splash") { await SceneManager.LoadSceneAsync("SplashScreen"); await Task.Delay(1200); return Capture("00-splash"); }
        if(action=="loading") { Core.Scene.SceneTransitor.Instance.LoadScene(Core.Scene.SceneDatabase.Instance.GetSceneByName("Game")); await Task.Delay(900); return Capture("05-chargement"); }
        if(action=="rebind") {
            var c=UnityEngine.Object.FindObjectsByType<Core.MainMenu.ControlOptionsController>().First();
            var bindings=(Core.MainMenu.ControlBindingUI[])Field(c,"bindings");
            bindings[0].button.onClick.Invoke(); await Task.Delay(300); var result=Capture("06-reaffectation");
            Call(c,"OnDisable"); return result;
        }
        var player=Find<Core.Player.PlayerController>();
        var interaction=player.updatables.OfType<Core.Player.PlayerInteraction>().First();
        if(action=="held") { interaction.GiveItem(Core.Item.ItemDatabase.Instance.buyableItems.First().GetHoldItem()); await Task.Delay(900); return Capture("53-objet-porte"); }
        if(action=="cook") { var cook=Find<Core.Interaction.CookInteractable>(); cook.AddItem(Core.Item.Cook.CookDatabase.Instance.Database.First().inputItem.GetHoldItem()); await Task.Delay(1000); return Capture("54-cuisson"); }
        if(action=="merge") {
            var brew=Find<Core.Interaction.BrewInteractable>();
            var recipe=Core.Item.Merge.MergeDatabase.Instance.Database.OfType<Core.Item.Merge.MergeVirusRecipe>().First(r=>r.GetResultItem().Item.itemName=="Poliovirus");
            foreach(var input in recipe.inputItems) brew.AddItem(input.GetHoldItem());
            var center=brew.GetComponent<Collider>().bounds.center;
            player.GetComponent<Rigidbody>().position=new Vector3(center.x,player.transform.position.y,center.z-1.1f); player.body.rotation=Quaternion.identity;
            await Task.Delay(700);
            float end=Time.realtimeSinceStartup+1.5f; int last=-1;
            while(Time.realtimeSinceStartup<end) { if(last!=Time.frameCount) {brew.InteractHold(player); last=Time.frameCount;} await Task.Yield(); }
            return Capture("55-melange");
        }
        if(action=="send-ok"||action=="send-wrong") {
            var send=Find<Core.Interaction.SendItemInteractable>();
            var item=action=="send-ok"?Core.Brief.BriefController.Instance.actualBrief.wantedItem:Core.Item.ItemDatabase.Instance.buyableItems.First();
            send.AddItem(item.GetHoldItem()); send.SendItem(); await Task.Delay(1550);
            return Capture(action=="send-ok"?"56-expedition-validee":"57-expedition-refusee");
        }
        return "Unknown moment";
    }
    public static async Task<object> Resolution(int width,int height) {
        var assembly=typeof(Editor).Assembly;
        var sizesType=assembly.GetType("UnityEditor.GameViewSizes");
        var singleton=typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
        var sizes=singleton.GetProperty("instance").GetValue(null);
        var group=sizesType.GetMethod("GetGroup").Invoke(sizes,new object[]{GameViewSizeGroupType.Standalone});
        var sizeType=assembly.GetType("UnityEditor.GameViewSize");
        var enumType=assembly.GetType("UnityEditor.GameViewSizeType");
        var count=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null);
        int selected=-1;
        for(int i=0;i<count;i++) {
            var existing=group.GetType().GetMethod("GetGameViewSize").Invoke(group,new object[]{i});
            if((int)sizeType.GetProperty("width").GetValue(existing)==width && (int)sizeType.GetProperty("height").GetValue(existing)==height) {selected=i;break;}
        }
        if(selected<0) {
            var size=Activator.CreateInstance(sizeType,new object[]{Enum.ToObject(enumType,1),width,height,"Visual review"});
            group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size}); selected=count;
        }
        var view=EditorWindow.GetWindow(assembly.GetType("UnityEditor.GameView"));
        view.GetType().GetProperty("selectedSizeIndex").SetValue(view,selected);
        view.Repaint(); await Task.Delay(1200);
        return new { width=Screen.width,height=Screen.height };
    }
    public static async Task<object> Run(string action)
    {
        Application.runInBackground=true;
        if(action.StartsWith("load:")) { Time.timeScale=1; await SceneManager.LoadSceneAsync(action.Substring(5)); await Task.Delay(1600); }
        if(action=="choice") Find<Core.MainMenu.OnBoardingMainMenuController>().OpenPanel();
        if(action=="choice-close") Find<Core.MainMenu.OnBoardingMainMenuController>().ClosePanel();
        if(action=="options") Find<Core.MainMenu.OptionsController>().OpenOption();
        if(action=="controls" || action=="audio") {
            var o=Find<Core.MainMenu.OptionsController>();
            var buttons=(Array)Field(o,"buttonWithPanel");
            var b=buttons.GetValue(action=="audio"?0:1);
            ((Button)Field(b,"button")).onClick.Invoke();
        }
        if(action=="options-close") Find<Core.MainMenu.OptionsController>().CloseOption();
        if(action=="accept") Find<Core.Brief.BriefInterface>().PutNewBrief();
        if(action.StartsWith("brief:")) {
            var brief=Core.Brief.BriefDatabase.Instance.Database.First(b=>b.briefTitle.Contains(action.Substring(6)));
            Find<Core.Brief.BriefInterface>().SetupNewBriefShow(brief);
        }
        if(action=="shop") Find<Core.Computer.ComputerInterface>().OpenPanel();
        if(action=="shop-close") Find<Core.Computer.ComputerInterface>().ClosePanel();
        if(action=="no-money") Find<Core.Computer.PathoNet.PathoNetInterface>().TriggerNoMoney();
        if(action=="library") Find<Core.MergeLibrary.MergeLibraryInterface>().OpenPanel();
        if(action=="library-close") Find<Core.MergeLibrary.MergeLibraryInterface>().ClosePanel();
        if(action=="analysis") Find<Core.Analysis.AnalysisInterface>().ShowAnalysis(Core.Brief.BriefController.Instance.actualBrief.wantedItem.GetHoldItem());
        if(action=="analysis-close") Find<Core.Analysis.AnalysisInterface>().ClosePanel();
        if(action=="scroll-bottom" || action=="scroll-middle") foreach(var s in UnityEngine.Object.FindObjectsByType<ScrollRect>()) s.verticalNormalizedPosition=action=="scroll-bottom"?0:0.5f;
        if(action=="cart") { foreach(var b in Find<Core.Computer.PathoNet.PathoNetInterface>().GetComponentsInChildren<Core.Computer.PathoNet.PathoNetBuyableItemBehaviour>()) b.AddItem(); }
        if(action=="stop-timer") Find<Core.Timer.TimerController>().StopTimer();
        if(action=="pause") Find<Core.Pause.PauseMenu>().OpenPanel();
        if(action=="pause-options") Find<Core.Pause.PauseMenu>().ActiveOptionsMenu();
        if(action=="pause-close") Find<Core.Pause.PauseMenu>().ClosePanel();
        if(action=="win") Find<Core.End.EndInterface>().OpenWinPanel();
        if(action=="lose") Find<Core.End.EndInterface>().OpenLosePanel();
        if(action.StartsWith("tutorial:")) {
            int index=int.Parse(action.Substring(9));
            var o=Find<Core.OnBoarding.OnBoardingController>();
            foreach(var f in o.GetType().GetFields(BindingFlags.Instance|BindingFlags.NonPublic).Where(f=>f.FieldType==typeof(GameObject))) {
                var go=(GameObject)f.GetValue(o); if(go) { go.SetActive(true); go.transform.localScale=Vector3.one; }
            }
            o.actualOnBoardingIndex=index; Find<Core.OnBoarding.OnBoardingInterface>().ShowActualBoard();
        }
        if(action=="recovery") Find<Core.OnBoarding.OnBoardingController>().RecoverLostItem(Core.OnBoarding.OnBoardingDatabase.Instance.itemToBuy1);
        await Task.Delay(action=="no-money"?100:1100);
        if(action=="inspect-brief") {
            var b=Find<Core.Brief.BriefInterface>();
            return new { prefab=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(b.gameObject), objects=b.GetComponentsInChildren<RectTransform>(true).Select(t=>new {name=t.name, parent=t.parent?.name, anchors=new {min=t.anchorMin.ToString(),max=t.anchorMax.ToString()},pos=t.anchoredPosition.ToString(),size=t.sizeDelta.ToString(), rect=t.rect.ToString(), text=t.GetComponent<TMP_Text>()?.text,font=t.GetComponent<TMP_Text>()?.fontSize}).ToArray() };
        }
        return new { scene=SceneManager.GetActiveScene().name, frame=Time.frameCount, resolution=Screen.width+"x"+Screen.height, action, texts=UnityEngine.Object.FindObjectsByType<TMP_Text>().Where(t=>t.enabled&&t.gameObject.activeInHierarchy).Select(t=>t.text).ToArray() };
    }
}
