import Hero from "./Hero";
import { orderBy } from "lodash";
import clsx from "clsx";

export default function Opponents({ players, opponentId }) {
  const sortedPlayers = orderBy(
    players,
    (player) => player.health + player.armor,
    "desc",
  );
  return (
    <div className="absolute top-0 bottom-0 left-2 flex scale-75 flex-col items-center justify-center gap-3">
      {sortedPlayers.map((player) => (
        <Hero
          {...player}
          key={player.id}
          type="opponents"
          className={clsx(
            opponentId === player.id && "ml-16",
            player.isDead && "opacity-60",
          )}
        />
      ))}
    </div>
  );
}
