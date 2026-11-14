# OVERWORKED

## Narrative Document — English

> English translation of [NARRATIVE.md](NARRATIVE.md) (original: [NARRATIVE.docx](NARRATIVE.docx)).
> This is the text that goes into the game (`Assets/_Project/Data/`). Machine translation
> (Claude) — the team proofreads it.
>
> Team decisions from the [project passport](../PROJECT_PASSPORT.md) are already applied:
> title **Overworked**, hero **Danny**, the break room **is Room 102** (opened with the key
> from Room 101), the letters **"DD"** flash on the Room 201 monitor.
>
> Legend: **DANNY** — Danny's line (subtitles), **MAX** — Max's message, **BOSS** — Boss's
> message, **DESC** — object description text, **SYSTEM** — on-screen system text,
> `{name}` — the player's name. Bracketed notes in *italics* are stage directions, not shown in game.

### GENERAL OVERVIEW AND STORYTELLING RULES

- **Genre and format:** 2D side-view psychological horror for PC. The focus is on atmosphere, exploration, dialogue and a slow build-up of tension.

- **Setting:** a three-storey corporate office. Each floor looks worse than the one before as Danny's sanity drops. Main colours: dirty green, grey and faded blue. The lighting becomes more and more unstable.

- **Characters:**

- **Danny (hero):** a tired office worker who just wants to finish his shift and get home. His lines are restrained, sometimes a little sarcastic. As the story goes on, he notices more and more that something is wrong.

- **Max (colleague):** Danny's co-worker. Talks to him through the phone. On the third floor he writes casually, as usual; on the second his messages become anxious and strange; on the first they turn almost entirely into glitch symbols.

- **Boss:** Danny's manager. Writes through the phone and keeps increasing the pressure. At first his messages look like ordinary work instructions, but over time they become more personal and start addressing the player directly.

- **Silhouettes:** phantom figures that appear during hallucinations. The final figure resembles Danny himself.

- **Player name ({name}):** entered before the game starts, in a corporate login window. The name is saved and used in the Boss's messages, Danny's lines and fourth-wall-breaking moments.

- **Core loop:** the player explores a floor, receives messages from Max, completes tasks, faces hallucinations and moves towards the stairs. The phone is opened manually; messages can be re-read but not replied to.

- **Controls:** move — A/D or arrow keys; interact — E; flashlight — F; phone — Tab or the on-screen button; pause — Esc. While the phone is open, Danny doesn't move.

**Mini-games:**

- **Printing the report (3rd floor):** the player presses five highlighted keys. At 60% progress the text gets corrupted and the bar resets.

- **Card reader (3rd floor):** the player holds E and releases it when the slider is in the green zone.

- **Monitor signal tuning (2nd floor):** two sliders — "Frequency" and "Sharpness". The player tunes the image to read a hidden message.

- **Tiny UFO (final mini-game):** launched in the break room (Room 102). Play / Quit menu and a single wave — a copy of Tiny UFO's fourth wave. Winning completes the task. Quit takes the player away from the arcade machine without completing the task.

# PROLOGUE. LOGIN

After pressing "New Game", the screen goes dark. The old corporate terminal boots up like an Arch Linux machine: kernel messages with timestamps race up the screen, then system services report in one by one. Any key skips the boot log.

**SYSTEM (boot log, abridged):**  
[    0.000000] Linux version 6.9.4-wtts1-1 … shift=night  
[    0.004118] DMI: OVERWORK Corp. Office Terminal WS-114, BIOS 4.02  
[    1.010532] systemd[1]: Hostname set to <floor03-ws114>.  
[  OK  ] Started Badge Reader Daemon.  
[  OK  ] Started Light Control (Floor 03).  
[FAILED] Failed to start Main Exit Door Controller.  
[  OK  ] Started Night Shift Service.  
[  OK  ] Reached target Floor 03.  
[ WARN ] Operator not registered.

*The failed exit door quietly foreshadows the locked main exit on the first floor.* The screen clears, and the login screen is typed out:

**SYSTEM:**  
WTTS v.4.02 — Work Time Tracking System (tty1)  
Night shift requires mandatory operator registration.  
Enter your name to confirm access.  
Enter your name: █

The player types a name and presses Enter. The system saves it as {name}.

**SYSTEM:**  
[  OK  ] Operator registered: {name}  
WELCOME TO THE STAFF, {name}.  
DO NOT LEAVE YOUR WORKPLACE.

The screen goes black. The hum of fluorescent lamps and the clatter of a keyboard can be heard.

# ACT 1. THIRD FLOOR

**Sanity: 100 → 90**

**Floor atmosphere:** the office looks familiar, but almost everyone has gone home. A few lamps flicker, monitors light up empty desks. There is only one separate room on this floor — the kitchen; the rest is the open space, the corridor, the turnstile and the stairs. The strangeness is still minor: ringing in the ears, ripples in the eyes and the phantom sound of keyboards.

## SCENE 1.0. THE BEGINNING

Danny sits at his computer, finishing yet another report.

**Mini-game 1 — "Printing the report".**  
A text editor window with a progress bar is open on screen. The player presses five highlighted keys. At 60% the line freezes, the text turns into a jumble of symbols, and the bar resets to zero with an error sound.

Danny stops typing.

**DANNY:** Damn… It's late already.  
**DANNY:** Stupid report. How many is that this month?  
**DANNY:** Fine. I'll finish it tomorrow.

Danny gets up from his chair. Interface elements appear one by one:
- Sanity bar and heartbeat indicator.
- Phone — for reading messages from Max and the Boss.
- Controls hint.

After the hint is closed, the player gets control.

## SCENE 1.1. OPEN SPACE AND CHARLES'S DESK

The player walks between rows of computers. Most of the desks are empty.

**DANNY:** Everyone left ages ago.  
**DANNY:** As always. Just me, stuck here till midnight.

One desk stands out from the rest: it is buried in papers and trash. The nameplate says "Charles".

When the player approaches, a description appears:

**DESC:** Of all the desks, this one draws the most attention.  
**DESC:** A pile of trash, crumpled sheets and a dusty nameplate: Charles.  
**DESC:** There's a note stuck to the monitor.

Hint: **E — Take the note.**

Full-screen text appears:

**SYSTEM:** THE BOSS DOESN'T LIKE IT WHEN I LEAVE BEFORE HIM!!!

**DANNY:** That's for sure…  
**DANNY:** Haven't seen Charles in a while. Where did he go?  
**DANNY:** Nobody even said whether he got fired or transferred.

Notification: **Received: Charles's note.**

The player heads towards the corridor. At the exit, the phone vibrates and Danny stops.

**NEW MESSAGE FROM MAX**

**MAX:** Helloooo, Danny.  
**MAX:** You there?  
**MAX:** How long are you gonna keep working?  
**MAX:** Since you're still in the office, grab my pass from the kitchen.  
**MAX:** Think I left it by the coffee machine.

The player closes the phone.

**DANNY:** Fine. At least someone remembered I'm here.

A task appears in the top right: **Get Max's pass.**

The player explores the corridor. Most doors are closed. Trying to enter the wrong door shows a notification: **"This doesn't look like the kitchen."**

Optional interactive objects:

- **Wall clock.**  
  **DESC:** The hands are frozen at 23:59. The second hand twitches in place, barely noticeably.  
  **DANNY:** Battery's dead.

- **Empty water cooler.**  
  **DESC:** A few cloudy drops are left at the bottom of the bottle. No cups.  
  **DANNY:** Great. Can't even get a drink of water.

- **"Employee of the Month" board.**  
  **DESC:** A faded photo of Charles hangs under the glass. Caption: "14 months without a day off".  
  **DANNY:** Hope they at least paid him extra for that.

## SCENE 1.2. THE KITCHEN

The player enters a small kitchen: dim light, green tiles and a constantly humming fridge.

A plastic card lies on the shelf next to the coffee machine.

Hint: **E — Pick up the pass.**

After the interaction the card disappears from the scene, and the task is marked as completed.

**DANNY:** Got it.

## SCENE 1.3. CORRIDOR AND TURNSTILE

The player leaves the kitchen. The phone vibrates again.

**NEW MESSAGE FROM MAX**

**MAX:** Thanks, buddy!!!  
**MAX:** You're the best.  
**MAX:** Go home already lol.  
**MAX:** I'm stuck on the floor below, I'll head out soon too.

The player closes the phone.

**DANNY:** Yeah. Good luck to both of us.

Danny walks up to the turnstile. Hint: **E — Use your card.**

A sharp error beep sounds.

**DESC:** ACCESS DENIED.

**DANNY:** What?..

The phone vibrates.

**NEW MESSAGE FROM THE BOSS**

**BOSS:** Danny, have you finished the report?  
**BOSS:** No?  
**BOSS:** Then where do you think you're going?  
**BOSS:** Your pass is suspended until the timesheet is closed.  
**BOSS:** Don't make me check your workplace in person.

After the phone is closed, the first hallucination begins.

**Hallucination 1 — Sanity: 100 → 90.**  
The lamps flash sharply. The image blurs, a ringing starts in the ears. For a few seconds, the loud clatter of dozens of keyboards is layered over the office sounds. The heartbeat speeds up, the sanity bar smoothly drops.

**DANNY:** My ears are ringing…  
**DANNY:** Damn Boss. Gets to me even from here.

The player goes back to the turnstile.

Hint: **E — Use Max's card.**

**Mini-game 2 — "Card reader".**  
A vertical bar with a moving slider appears. The player holds E and releases it when the slider is in the green zone.

A short green beep sounds.

**DESC:** ACCESS GRANTED.

**DANNY:** It worked. Sorry, Max.

The player walks up to the stairs.

Hint: **E — Go down to the second floor.**

The screen slowly fades to black.

# ACT 2. SECOND FLOOR

**Sanity: 90 → 50**

**Floor atmosphere:** almost all the lamps have burned out. The corridor is sunk in dirty green and blue shadows. The main light source is Danny's flashlight. The first black silhouettes appear, along with more noticeable distortions of the surroundings.

## SCENE 2.0. DARK CORRIDOR AND THE FLASHLIGHT

The fade clears. Danny stands on the second-floor landing. It's almost pitch black.

**DANNY:** So dark… Did they just shut off the power here?  
**DANNY:** I hope somebody knows what's going on.

The flashlight is already in Danny's hands. Hint: **F — Turn the flashlight on / off.**

When the player turns the flashlight on, the phone starts vibrating. Without turning it on, the player can't go any further.

**NEW MESSAGE FROM MAX**

**MAX:** Danny, wait.  
**MAX:** You went down to the second, right?  
**MAX:** The exit downstairs is locked.  
**MAX:** The system won't close the shift without an archive check.  
**MAX:** The documents are in room 201.  
**MAX:** Please check them. I can't get in there.

A message from the Boss follows.

**NEW MESSAGE FROM THE BOSS**

**BOSS:** You're wasting time on the wrong tasks, Danny.  
**BOSS:** The archive in 201 is none of your business.  
**BOSS:** Never mind.  
**BOSS:** You'll find out what happens to people who can't set priorities.  
**BOSS:** No bonus for you this month.

A task appears: **Check the documents in room 201.**

**DANNY:** Yeah. Like I was ever getting one.  
**DANNY:** I'll help Max and finally get out of here.

## SCENE 2.1. THE SILHOUETTE HALLUCINATION

The player moves down the dark corridor, lighting the way with the flashlight.

Optional objects to inspect:

- **Broken lamp.**  
  **DESC:** Shards of a glass tube are scattered across the carpet. It fell very recently.  
  **DANNY:** Better watch my step…

- **Forgotten mug.**  
  **DESC:** Dried coffee inside, covered in grey mould.  
  **DANNY:** This mug's been sitting here for two weeks.

In the middle of the corridor, the player enters a trigger zone.

**Hallucination 2 — Sanity: 90 → 70.**  
At the edge of the flashlight beam, a motionless, hunched human silhouette appears, standing with its face to the wall. If the player comes closer or points the flashlight at it, a sharp sound plays. The image distorts, the heartbeat spikes, and the silhouette vanishes.

**DANNY:** What was that?..  
**DANNY:** Someone was standing there. I saw it.

Danny tries to keep going. For a short while the controls behave strangely: the character keeps walking in the chosen direction after the key is released.

**DANNY:** Wait…  
**DANNY:** I let go of the button.

Danny stops and looks towards the screen.

**DANNY:** Was that you?  
**DANNY:** Weird. Okay… I'm probably just tired.

The controls return to normal. Danny continues towards room 201.

## SCENE 2.2. ROOM 201

The player walks up to the door.

Hint: **E — Enter room 201.**

The screen fades. Danny is inside an office: metal filing cabinets, a desk with drawers and a dimly glowing computer.

Inspect the drawer — **E**.

Inside is a thick folder of shift schedules.

**DESC:** The same names repeat on every sheet: Charles, Max, Danny…  
**DESC:** At the very bottom, a fresh line: {name} — Status: Active.

**DANNY:** What is this nonsense…  
**DANNY:** Who's {name}? Nobody by that name ever worked in our department.

Danny pauses.

**DANNY:** Wait. That's your name, isn't it?  
**DANNY:** How did it get here?..

The player walks up to the computer.

Hint: **E — Check the computer.**

**Mini-game 3 — "Monitor signal tuning".**  
The screen is full of static and ripples. At the bottom are two sliders: "Frequency" and "Sharpness". The player adjusts the image until it becomes clear.

A system message appears on the screen:

**SYSTEM:**  
REPORT NOT CLOSED.  
SHIFT CONTINUES.

The task "Check the documents in room 201" is marked as completed.

**Hallucination 3 — Sanity: 70 → 50.**  
The door behind slams shut with a crash. The light starts flickering wildly, static covers the computer screen. For an instant, two letters show through the static — **DD** — and then the screen goes dark. A sharp sound hit, the heartbeat speeds up.

**DANNY:** What the hell is going on here?!

After a short pause, the door opens.

The phone starts vibrating non-stop.

**NEW MESSAGE FROM MAX**

**MAX:** Danny, did you find the papers??  
**MAX:** I can hear footsteps downstairs.  
**MAX:** He's looking right at me.  
**MAX:** DON'T GO DOWN TO THE FIRST FLOOR!!!

A message from the Boss follows.

**NEW MESSAGE FROM THE BOSS**

**BOSS:** You see what happens when you act on your own, Danny?  
**BOSS:** Max couldn't handle the workload. He's been demoted.  
**BOSS:** But you have an advantage.  
**BOSS:** You are being guided by {name}.  
**BOSS:** A good employee. Choose the right friends.  
**BOSS:** Come down to the first floor. We need to close your shared file.

Danny looks at the phone.

**DANNY:** No… No, I don't want to go there.  
**DANNY:** You hear me? Don't take me down there.

The player leaves the room.

When trying to turn back, a notification appears:

**SYSTEM:** "Do you still want Danny to keep working here?"

Danny stops for a moment.

**DANNY:** I just want to go home.

The player heads to the stairs.

Hint: **E — Go down to the first floor.**

The screen fades to black.

# ACT 3. FIRST FLOOR

**Sanity: 50 → 0**

**Floor atmosphere:** the office looks unnaturally stretched. The walls are covered in dark streaks, the remaining lamps flicker constantly. The hum of wires and Danny's heavy breathing can be heard. Max's messages turn almost entirely into unreadable symbols. The Boss starts addressing the player directly.

## SCENE 3.0. FIRST-FLOOR CORRIDOR

The fade clears. Danny stands in the first-floor corridor. Ahead are the glass doors of the main exit and the heavy door of Room 102.

Check the exit — **E**.

**DESC:** The door is locked with an electronic lock. Behind the glass — absolute black emptiness. No street, no streetlights.

**DANNY:** There's nothing out there…

Check Room 102 — **E**.

**DESC:** The door is locked. The sign reads: "BREAK ROOM. KEY IS IN ROOM 101."

A task appears: **Find the key in room 101.**

After a few steps, the phone starts vibrating non-stop.

**NEW MESSAGE FROM MAX**

**MAX:** D̷a̵n̸n̶y̷ w̵h̵e̵r̵e̵ a̵r̵e̵ y̵o̵u̵ 01001100 01001111  
**MAX:** █▓▒░ HE'S HERE ░▒▓█  
**MAX:** ⸸☥☦SDS ▓▒░░DVC,SDLV,;FD▒▓  
**MAX:** ▒▒▒░░░ R̷E̷P̷O̷R̷T̷ N̷O̷T̷ C̷L̷O̷S̷E̷D̷ ░░░▒▒▒

A message from the Boss follows.

**NEW MESSAGE FROM THE BOSS**

**BOSS:** Look at him, {name}.  
**BOSS:** He can barely stand.  
**BOSS:** Danny is a valuable and diligent employee.  
**BOSS:** Well done for guiding him.  
**BOSS:** You won't close the window, will you, {name}?  
**BOSS:** You're too curious how this all ends.  
**BOSS:** Take him to room 101. Close the shift, and I'll let you both go.

Danny stops. Now he is looking straight at the player.

**DANNY:** Did you hear him, {name}?  
**DANNY:** He's talking about you.  
**DANNY:** You do understand I don't want to go there?

Danny falls silent for a moment.

**DANNY:** I don't know who you are. But could you at least stop leading me where he tells you to?

The player regains control.

Optional objects to inspect:

- **Dark glass by the security desk.**  
  **DESC:** In the reflection, Danny's face looks haggard. Deep shadows under his eyes.  
  **DANNY:** I don't even recognise myself…

- **Stack of forms on a cabinet.**  
  **DESC:** Resignation forms. Each one has a red stamp: "DENIED".  
  **DANNY:** Nobody ever got out of here.

## SCENE 3.1. ROOM 101

The player walks up to the door.

Hint: **E — Enter room 101.**

After a fade, Danny is inside.

**Hallucination 4 — Sanity: 50 → 25.**  
The lamps start flickering in time with the heartbeat. A dark silhouette of a worker appears in the corner. As the player approaches, a sharp audio hit plays, the image distorts heavily, the pulse spikes.

**DANNY:** Stop it…  
**DANNY:** Please. I can't take it anymore.

In the corner, a man sits on a chair, mumbling incoherently. When the player approaches, a hint appears: **E — Interact.**

After pressing E, the figure shrinks into a single point and disappears.

Danny stares at the empty spot for a while.

**DANNY:** He wasn't here…

The player walks up to the cabinet.

Hint: **E — Open the drawer.**

Inside are a heavy key to room 102 and a cracked plastic badge.

**DESC:** The badge reads {name}, position: "Senior Operator".

**DANNY:** Senior operator?..  
**DANNY:** No. This isn't funny anymore.

Notification: **Received: Room 102 key.**

The task "Find the key in room 101" is marked as completed.

A new task appears: **Finish work in room 102.**

The phone vibrates again.

**NEW MESSAGE FROM MAX**

**MAX:** ▓▒░ 01110011 01110100 01101111 01110000 ░▒▓  
**MAX:** █▀█▀██UUUUUUUUYUTSAPPPPPPTLSHHHH▀█▀█  
**MAX:** ░▒▓█DKPUDKBPUTSUTSTSUAUTSUTS█▓▒░

**NEW MESSAGE FROM THE BOSS**

**BOSS:** Excellent, {name}.  
**BOSS:** You have the key.  
**BOSS:** One last task left.  
**BOSS:** Danny has to finish his shift.  
**BOSS:** Send him to room 102, the break room. He needs a short break before the work is done.

## SCENE 3.2. ROOM 102 — BREAK ROOM AND TINY UFO

The player leaves Room 101 and walks up to the door of Room 102.

**DANNY:** The break room? Now?  
**DANNY:** Well, finally something sensible tonight.

Hint: **E — Unlock and enter room 102.**

Danny goes inside. The room looks almost normal: a table, a few chairs, a vending machine and an old arcade machine.

The phone vibrates.

**NEW MESSAGE FROM THE BOSS**

**BOSS:** Sit down, Danny.  
**BOSS:** You've worked hard today.  
**BOSS:** A few minutes of rest won't hurt.  
**BOSS:** The machine is already on. You can play one round.

**DANNY:** Never thought I'd hear that from him.

Danny walks up to the arcade machine.

Hint: **E — Play.**

Tiny UFO opens.

**Mini-game 4 — Tiny UFO.**  
A menu with Play and Quit buttons appears. Play starts a single wave — a copy of the original game's fourth wave. Danny's controls are locked during the mini-game.
- Winning the wave completes the mini-game.
- On defeat, the wave restarts.
- Quit closes the machine and returns the player to the break room without completing the task.

After the win, the arcade screen goes dark.

**DANNY:** That's it?  
**DANNY:** Weird. I can't even remember the last time I played anything.

Danny leaves the break room.

## SCENE 3.3. THE FINAL SCREAMER

As soon as Danny steps into the corridor, a sharp sound comes from behind him. The camera jerks.

A phantom figure of a worker appears ahead. At first the silhouette seems ordinary, but as it gets closer it becomes clear that it is twisted and resembles Danny himself. Its movements are unnatural, its proportions wrong.

Danny freezes.

**DANNY:** No…

The silhouette lunges at Danny. The figure fills the camera, a loud audio hit plays.

**Final hallucination — Sanity: 25 → 0.**  
The sanity bar empties fast. The heartbeat becomes deafening. The image loses focus, controls are completely locked.

Danny collapses. The screen slowly goes dark.

**DANNY:** {name}…  
**DANNY:** I can't anymore…

Danny loses consciousness.

The sound cuts off abruptly.

# ACT 4. FINAL EVENT AND THE WHITE ROOM

## SCENE 4.0. THE WHITE ROOM

After a long pause, a sterile white space appears. No walls, no ceiling — only a white background and the floor line. The HUD is hidden; only the sound of the heartbeat remains.

Danny stands in the middle of the room. He looks calmer than before, but still very tired. The player gets control. Danny can only move to the right.

He stays silent for a while, then speaks to the player.

**DANNY:** {name}…  
**DANNY:** Are you still here?

Danny keeps walking.

**DANNY:** I don't understand where we are.  
**DANNY:** This isn't the office anymore, is it?

A bed appears. Danny notices it and stops.

**DANNY:** I think I know this place.

He keeps walking towards the bed.

**DANNY:** I kept thinking that if I just finished the work, I could rest.  
**DANNY:** One more report. One more shift. Just a little longer.

A short pause.

**DANNY:** And then I could go home.

Danny walks closer to the bed.

**DANNY:** Funny. I can't even remember the last time I slept properly.

The pulse slowly drops: 40 → 30 → 20 → 10.

**DANNY:** {name}…  
**DANNY:** Thank you for staying with me till the end.

Danny lies down on the bed. This happens automatically when he gets close.

The camera stays still. Danny closes his eyes.

In the silence, the mechanical sound of a ventilator and the rhythmic beeping of a heart monitor start to be heard. It becomes clear that the white room is an image of a hospital ward, and that Danny has been in critical condition all along.

The pulse drops to zero.

A long, continuous heart-monitor tone sounds.

The screen slowly dissolves into white light.

The end credits appear.
