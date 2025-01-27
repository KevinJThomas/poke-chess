import Pokemon from "./Pokemon";

export default function PlayerPokemon({ board }) {
  return (
    <>
      {board?.map((pokemon) => (
        <Pokemon key={pokemon.id} {...pokemon} />
      ))}
    </>
  );
}
