import Hero from "./Hero";
import { orderBy } from "lodash";
import clsx from "clsx";

export default function Opponents({ playersMap, opponentId }) {
  const playerIds = Object.keys(playersMap);
  const sortedPlayers = orderBy(
    playerIds,
    [
      (playerId) => playersMap[playerId].health + playersMap[playerId].armor,
      (playerId) => playersMap[playerId].name,
    ],
    "desc",
  );
  return (
    <div className="absolute top-0 bottom-0 left-2 flex scale-75 flex-col items-center justify-center gap-3">
      {sortedPlayers.map((playerId) => (
        <Hero
          {...playersMap[playerId]}
          key={playersMap[playerId].id}
          type="opponents"
          className={clsx(
            opponentId === playersMap[playerId].id && "ml-16",
            playersMap[playerId].isDead && "opacity-60",
          )}
        />
      ))}
    </div>
  );
}
