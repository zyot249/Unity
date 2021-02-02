package sfs2x.extension.mmo;

import java.util.Arrays;
import java.util.HashMap;
import java.util.LinkedList;
import java.util.List;
import java.util.Map;
import java.util.Random;
import java.util.concurrent.ScheduledFuture;
import java.util.concurrent.TimeUnit;

import com.smartfoxserver.v2.SmartFoxServer;
import com.smartfoxserver.v2.api.ISFSMMOApi;
import com.smartfoxserver.v2.core.ISFSEvent;
import com.smartfoxserver.v2.core.SFSEventParam;
import com.smartfoxserver.v2.core.SFSEventType;
import com.smartfoxserver.v2.entities.User;
import com.smartfoxserver.v2.entities.variables.SFSUserVariable;
import com.smartfoxserver.v2.entities.variables.UserVariable;
import com.smartfoxserver.v2.exceptions.SFSException;
import com.smartfoxserver.v2.extensions.BaseServerEventHandler;
import com.smartfoxserver.v2.extensions.SFSExtension;
import com.smartfoxserver.v2.mmo.MMORoom;
import com.smartfoxserver.v2.mmo.Vec3D;

public class MMORoomDemoExtension extends SFSExtension
{
	private class UserVariablesHandler extends BaseServerEventHandler
	{
		@Override
		public void handleServerEvent(ISFSEvent event) throws SFSException
		{
			@SuppressWarnings("unchecked")
            List<UserVariable> variables = (List<UserVariable>) event.getParameter(SFSEventParam.VARIABLES);
			User user = (User) event.getParameter(SFSEventParam.USER);
			
			// Make a map of the variables list
			Map<String, UserVariable> varMap = new HashMap<String, UserVariable>();
			for (UserVariable var : variables)
			{
				varMap.put(var.getName(), var);
			}
			
			if (varMap.containsKey("x") && varMap.containsKey("z"))
			{
				Vec3D pos = new Vec3D
				(
					varMap.get("x").getDoubleValue().floatValue(),
					1.0f,
					varMap.get("z").getDoubleValue().floatValue()
				);
				
				mmoAPi.setUserPosition(user, pos, getParentRoom());
			}
		}
	}
	
	private class NPCRunner implements Runnable
	{
		@Override
		public void run()
		{
			for (User npc : allNpcs)
			{
				NPCData data = (NPCData) npc.getProperty("npcData");
				
				double xpos = npc.getVariable("x").getDoubleValue();
				double zpos = npc.getVariable("z").getDoubleValue();
				
				double newX = xpos + data.xspeed;
				double newZ = zpos + data.zspeed;
				
				// Check Map X limits
				if (newX < -100 || newX > 100)
				{
					newX = xpos;
					data.xspeed *= -1;
				}
				
				// Check Map Z limits
				if (newZ < -100 || newZ > 100)
				{
					newZ = zpos;
					data.zspeed *= -1;
				}
				
				// Set NPC variables
				List<UserVariable> vars = Arrays.asList
				(
					(UserVariable)
					new SFSUserVariable("x", newX), 
					new SFSUserVariable("z", newZ)
				);
				
				getApi().setUserVariables(npc, vars);
			}
		}
	}
	
	private final static class NPCData
	{
		public float xspeed;
		public float zspeed;
	}
	
	//-------------------------------------------------------------------------------
	
	private static final int MAX_NPC = 50;
	private static final int MAX_MAP_X = 100;
	private static final int MAX_MAP_Z = 100;
	
	
	private UserVariablesHandler userVariablesHandler;
	private NPCRunner npcRunner;
	private ISFSMMOApi mmoAPi;
	
	private ScheduledFuture<?> npcRunnerTask;
	private List<User> allNpcs;
	
	@Override
	public void init()
	{
	    userVariablesHandler = new UserVariablesHandler();
	    npcRunner = new NPCRunner();
	    mmoAPi = SmartFoxServer.getInstance().getAPIManager().getMMOApi();
	    
	    addEventHandler(SFSEventType.USER_VARIABLES_UPDATE, userVariablesHandler);
	    
	    try
        {
	        simulatePlayers();
        }
        catch (Exception e)
        {
        	e.printStackTrace();
        }
	}
	
	@Override
	public void destroy()
	{
	    super.destroy();
	    
	    // Destroy NPCRunnerTask
	    if (npcRunnerTask != null)
	    	npcRunnerTask.cancel(true);
	}
	
	private void simulatePlayers() throws Exception
	{
		allNpcs = new LinkedList<User>();
		Random rnd = new Random();
		
		MMORoom mmoRoom = (MMORoom) getParentRoom();
		
		for (int i = 0; i < MAX_NPC; i++)
        {
			allNpcs.add(getApi().createNPC("NPC#" +i, getParentZone(), false));
        }
		
		for (User user : allNpcs)
        {
			int rndX = rnd.nextInt(MAX_MAP_X);
			int rndY = 1;
			int rndZ = rnd.nextInt(MAX_MAP_Z);
			
			// head or tails
			if (rnd.nextInt(100) > 49)
				rndX *= -1;
						
			if (rnd.nextInt(100) > 49)
				rndZ *= -1;
					
			Vec3D rndPos = new Vec3D((float) rndX, (float) rndY, (float) rndZ);
			
			List<UserVariable> uVars = Arrays.asList
			(
				(UserVariable) new SFSUserVariable("x", (double) rndX),
				new SFSUserVariable("y", (double) rndY),
				new SFSUserVariable("z", (double) rndZ),
				new SFSUserVariable("rot", (double) rnd.nextInt(360)),
				new SFSUserVariable("model", 2),
				new SFSUserVariable("mat", rnd.nextInt(3))
			);
			
			NPCData data = new NPCData();
			data.xspeed = rnd.nextFloat() * 1.2f;
			data.zspeed = rnd.nextFloat() * 1.2f;
			
			user.setProperty("npcData", data);
					
			// Set Vars
			getApi().setUserVariables(user, uVars);
			
			// Join Room
			getApi().joinRoom(user, mmoRoom);
			
			// Set User Position
			mmoAPi.setUserPosition(user, rndPos , mmoRoom);
        }
		
		// Start NPC Task
		npcRunnerTask = SmartFoxServer.getInstance().getTaskScheduler().scheduleAtFixedRate
		(
			npcRunner, 			
			0, 							// 0 initial delay
			100, 						// run every 100ms
			TimeUnit.MILLISECONDS		
		);
	}
}
