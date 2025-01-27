import Hero from "./Hero";

export default function Players({ players }) {
  return (
    <div className="absolute top-0 bottom-0 left-2 flex scale-75 flex-col items-center justify-center gap-3">
      {players.map((player) => (
        <Hero
          key={player.id}
          name={player.name}
          armor={player.armor}
          health={player.health}
          tier={player.tier}
        />
      ))}
    </div>
  );
}
