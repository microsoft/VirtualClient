import React from 'react';
import clsx from 'clsx';
import styles from './styles.module.css';

const FeatureList = [
  {
    title: 'Cross-Platform, Cross-Distribution. Comprehensively powerful',
    Svg: require('@site/static/img/undraw/undraw_connected_world_wuay.svg').default,
    description: (
      <>
        Support X64, ARM64, Windows, various Linux distributions. Supports wide range of benchmarks covering CPU, GPU, Memory, IO, Network and more.
      </>
    ),
  },
  {
    title: 'Designed for largescale A/B cloud testing.',
    Svg: require('@site/static/img/undraw/undraw_split_testing_l1uw.svg').default,
    description: (
      <>
        VC is designed to run multiple iterations cross instances, to reduce variance and increase confidence scores.
      </>
    ),
  },
  {
    title: 'Deterministic configurations by experts.',
    Svg: require('@site/static/img/undraw/undraw_in_sync_re_jlqd.svg').default,
    description: (
      <>
        It is now easy to reproduce others' runs with deterministic configurations curated by performance experts.
      </>
    ),
  },
];

function Feature({Svg, title, description}) {
  return (
    <div className={clsx('col col--4')}>
      <div className="text--center">
        <Svg className={styles.featureSvg} role="img" />
      </div>
      <div className="text--center padding-horiz--md">
        <h3>{title}</h3>
        <p>{description}</p>
      </div>
    </div>
  );
}


const Index = () => {
  return (
    <section className={styles.features}>
      <div className="container">
        <div className="row">
          {FeatureList.map((props, idx) => (
            <Feature key={idx} {...props} />
          ))}
        </div>
      </div>
    </section>
  );
};

export default Index;