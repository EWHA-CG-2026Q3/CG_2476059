# S23 배포 자료 참고

- 출처: 사용자가 제공한 `EWHA_CG_2026Q3-main (6).zip`, `CG_S23_곡면 - Google Slides.pdf`.
- `.txt` 파일은 강의 배포 코드의 참고 사본이며 컴파일되지 않는다.
- `Shaders/S23_Reference_S20_*.shader`는 세 재질 실험을 위해 배포 S20 셰이더의 이름만 S23 전용으로 바꾼 사본이다.
- `S23_ToonVertexOutline.shader`는 배포된 ToonOutline의 2-Pass 구조를 참고하여 첫 Pass에 VertexColor 계산을 결합하였다. 최종 Shader는 정확히 2 Pass이며 UsePass를 추가하지 않는다.
- `Scripts/S23_BilinearPatch.cs`는 배포된 bilinear 보간식과 격자 삼각형 연결 방식을 바탕으로, 분할 수 제한과 생성 Mesh 수명 관리를 추가한 S23 전용 구현이다.
- 잎 Texture 3개는 ZIP의 ThirdParty/Textures에서 S23/Textures로 복사하였다. Base Color의 Alpha를 사용하며 AO/Normal 파일은 참고용으로 보존하였다.
- Pavilion은 프로젝트에 이미 있는 ThirdParty 원본을 참조한다. 원본 프리팹과 재질은 수정하지 않았다.
