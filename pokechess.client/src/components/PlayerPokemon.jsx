import Pokemon from "./Pokemon";

export default function PlayerPokemon({ board }) {
  return (
    <>
      {board?.map((pokemon) => (
        <Pokemon
          key={pokemon.id}
          name={pokemon.name}
          attack={pokemon.attack}
          health={pokemon.health}
          id={pokemon.id}
        />
      ))}
    </>
  );
}
