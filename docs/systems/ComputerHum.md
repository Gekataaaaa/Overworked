# ComputerHum — гул компьютеров

**Владелец:** Программист 1

**Что делает:** гул работающего компьютера (`Audio/Ambient/Resources/AMB_ComputerHum`, 23 с, по кругу без провала —
`SeamlessLoop`): звучит, пока в кадре камеры есть работающий компьютер; ушёл из кадра — плавно стихает (~0.5 с).
Как и звуки ламп — только для того, что в кадре.

**Как подключить:** на объект компьютера — компонент `ComputerHum` (галочка `working`). Сейчас стоит на
Floor_03 `Environment/Corridor/Props/Denny_Desk` (стол Дэнни) и Floor_02 `Environment/Rooms/Room_201/Props/Archive_Computer`.
Стол Чарльза (заброшен, монитор погашен) и аркада Tiny UFO — без гула. Играет `ComputerHumPlayer` — создаёт себя сам
на всю игру; настройки: `volume` (0.35), `fadeSpeed` (2), `viewMargin` (0.05). Компьютер «в кадре» — центр его спрайта.

**Публичный API:** `ComputerHum.SetWorking(bool)` — включить/выключить компьютер из сценария, `ComputerHum.Working`,
`ComputerHum.Active` — включённые метки сцены.

**Зависит от:** `AudioManager`, `SeamlessLoop`, `LampHum.InView`, `Camera.main`.

**Известные ограничения / TODO:**
- Громкость не зависит от расстояния и числа компьютеров в кадре.
